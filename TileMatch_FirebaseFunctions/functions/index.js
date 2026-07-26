//#region init
//사용할 파이어베이스 라이브러리 기본 설정
const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { setGlobalOptions } = require("firebase-functions/v2");
//FieldValue는 현재 활성 코드에서 미사용(주석 레거시에만 사용). 레거시 복원 시 다시 추가 필요.
const { getFirestore } = require("firebase-admin/firestore");
const admin = require("firebase-admin");
//[영수증 검증 제거] 완전 신뢰 모델이라 서버 영수증 검증 미사용. 구매는 클라 로컬 처리 후 saveData로 동기화.
//const { GoogleAuth } = require("google-auth-library");
admin.initializeApp();
const db = getFirestore();

///Functions의 전역 설정을 지정해줍니다.
///해당 Functions가 동작할 지역을 서울로 지정합니다.
///최대 운용 가능한 Functions의 개수를 3개로 제한합니다.
///하나의 Functions가 최대 80개의 작업을 처리할 수 있도록 지정합니다.
///(클라이언트에서 1회 호출 = Functions 1회 수행)을 방지하기 위한 처리입니다.
setGlobalOptions({
    region: "asia-northeast3",
    maxInstances: 3,
    concurrency: 80
})
//#endregion

//#region variable
//Functions 버전입니다. (유저 기본 데이터 스키마 버전)
const VERSION = 3;

//[영수증 검증 제거] 아래 구글 플레이 인앱결제 검증 설정은 미사용.
//const ANDROID_PACKAGE_NAME = "com.acidstudio.trumptile";
//const ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher";
//const googleAuth = new GoogleAuth({ scopes: [ANDROID_PUBLISHER_SCOPE] });

//(아래 상수들은 하단 주석 처리된 레거시 함수에서만 사용되던 값입니다.)
//const publicReference = "publicReference";
//const clearTimePath = "minTimeRequiredClearStage";
//const housingStarPath = "starRequiredProgressHousing";
//const publicStageReference = "publicStageReference";
//const publicHousingReference = "publicHousingReference";
//const itemPathArr = ["item.blackhole", "item.timer", "item.bomb"];
//const clearTimeFieldPathArr = ["normal","normal","normal","normal","normal","normal","normal","normal","normal","normal"];
//const clearThresholdsArr = [30, 50];
//#endregion

//====================================================================================
// 활성 onCall 함수 (명세 4케이스)
// 공통 클라 플로우: 네트워크 연결 시에만 호출 → 구글 로그인 → 로그인 플래그 →
//                  ID토큰으로 Firebase UID 확보 → 아래 Functions 호출
//====================================================================================

///[케이스1] 첫 접속/로그인 onCall 함수입니다.
///UID로 유저 문서(인스턴스)를 읽어옵니다. 문서가 없으면(첫 접속) 기본 데이터로 생성합니다.
///이후 호출들은 이미 확보된 인스턴스를 재사용하므로 이 과정을 생략합니다.
exports.progressLogin = onCall(async (request) => {
    const docRef = getDoc(request);
    const doc = await docRef.get();

    //이미 존재하는 유저: 저장된 데이터를 그대로 반환합니다.
    if (doc.exists) {
        return doc.data();
    }

    //첫 접속: 기본 데이터를 생성하고, 실제 저장된 값(서버 타임스탬프 포함)을 다시 읽어 반환합니다.
    await docRef.set(getDefaultUserData());
    const created = await docRef.get();
    return created.data();
});

///[케이스2] 플레이어 데이터 저장 onCall 함수입니다. (스테이지 클리어 등에서 호출)
///완전 신뢰 모델: 클라이언트가 보낸 데이터를 서버 검증 없이 merge 저장합니다.
///(주의: 안티치트가 없으므로 클라이언트가 보낸 값이 그대로 반영됩니다.)
exports.saveData = onCall(async (request) => {
    //getDoc이 인증(getUID)을 강제한다. 문서는 set(merge)로 없으면 새로 생성되므로 존재 검사는 하지 않는다.
    const docRef = getDoc(request);

    const payload = request.data;

    //객체가 아닌 잘못된 페이로드로 인한 크래시만 방지합니다. (값 자체는 검증하지 않음)
    if (!payload || typeof payload !== "object" || Array.isArray(payload)) {
        throw new HttpsError("invalid-argument", "저장할 데이터가 올바르지 않습니다.");
    }

    //(1) 유저 데이터(5개 필드) 저장 - 완전 신뢰 merge
    if (payload.user && typeof payload.user === "object") {
        await docRef.set(payload.user, { merge: true });
    }

    //(2) 리더보드 더미 데이터 저장 - 클라이언트가 생성한 더미 AI 경쟁자 스냅샷을 그대로 보관합니다.
    //    재설치 후 loadData로 복원하면 동일한 순위가 유지됩니다.
    //    형태: { data: <더미 리스트 JSON 문자열>, lastRefresh: <마지막 갱신 ticks 문자열> }
    if (payload.leaderboard && typeof payload.leaderboard === "object") {
        await docRef.set({ leaderboard: payload.leaderboard }, { merge: true });
    }

    return { success: true };
});

//[케이스3 - 영수증 검증 제거] 구매는 클라이언트가 Google Play Billing으로 로컬 처리한 뒤
//saveData로 서버에 반영합니다. 아래 서버 영수증 검증 함수는 미사용(보존).
/*
///[케이스3] 인앱 구매 onCall 함수입니다.
///클라이언트에게 productId와 purchaseToken을 넘겨받아 구글 Play Developer API로 영수증을 검증합니다.
///정상 결제인 경우 중복 지급을 방지한 뒤 상품에 맞는 처리를 하고 결제 기록을 남깁니다.
exports.purchaseProduct = onCall(async (request) => {
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const { productId, purchaseToken } = request.data || {};
    if (!productId || !purchaseToken) {
        throw new HttpsError("invalid-argument", "productId와 purchaseToken이 필요합니다.");
    }

    //구글 영수증 검증
    const purchase = await verifyGooglePurchase(productId, purchaseToken);

    //purchaseState: 0=구매완료, 1=취소, 2=보류
    if (purchase.purchaseState !== 0) {
        throw new HttpsError("failed-precondition", "완료되지 않은 결제입니다.");
    }

    //중복 지급 방지: purchaseToken을 키로 이미 처리한 결제인지 확인합니다.
    const purchaseRef = db.collection("purchases").doc(purchaseToken);
    const purchaseDoc = await purchaseRef.get();
    if (purchaseDoc.exists) {
        throw new HttpsError("already-exists", "이미 처리된 결제입니다.");
    }

    //상품에 맞는 처리(지급)를 수행합니다.
    await applyPurchaseReward(docRef, productId);

    //처리한 결제를 기록하여 재지급을 방지합니다.
    await purchaseRef.set({
        uid: getUID(request),
        productId: productId,
        orderId: purchase.orderId || null,
        purchaseTimeMillis: purchase.purchaseTimeMillis || null,
        processedAt: FieldValue.serverTimestamp()
    });

    return { success: true, productId: productId };
});
*/

///[케이스4] 데이터 불러오기 onCall 함수입니다.
///UID로 서버에 저장된 유저 데이터를 읽어 반환합니다.
exports.loadData = onCall(async (request) => {
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    return doc.data();
});

//====================================================================================
// 활성 헬퍼 함수 (클라이언트에서 직접 호출되지 않음)
//====================================================================================

///서버 함수를 호출한 유저의 UID를 반환합니다. 로그인되지 않은 경우 예외를 던집니다.
function getUID(request){
    // AppCheck 추가시 로직
    // if(request.app == undefined){
    //     throw new HttpsError("failed-precondition", "정품 앱에서 접근해주세요.");
    // }
    if(!request.auth){
        throw new HttpsError("unauthenticated", "로그인이 필요합니다.");
    }
    return request.auth.uid;
}

///서버 함수를 호출한 유저의 UID로 users 콜렉션의 UID 문서 경로를 반환합니다.
function getDoc(request){
    const uid = getUID(request);
    return db.collection("users").doc(uid);
}

///문서가 존재하는지 체크합니다. 존재하지 않으면 예외를 던집니다.
function checkDocExists(doc){
    if(!doc.exists)
    {
        throw new HttpsError("not-found", "데이터가 존재하지 않습니다.");
    }
}

/*
///[영수증 검증 제거로 미사용 - 보존]
///구글 Play Developer API로 인앱 상품 영수증을 검증하고 결제 정보를 반환합니다.
async function verifyGooglePurchase(productId, purchaseToken){
    const url = `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${ANDROID_PACKAGE_NAME}/purchases/products/${productId}/tokens/${purchaseToken}`;

    try {
        const client = await googleAuth.getClient();
        const res = await client.request({ url: url });
        return res.data;
    }
    catch (error) {
        throw new HttpsError("permission-denied", "영수증 검증에 실패했습니다.");
    }
}

///검증된 상품에 대한 처리(지급)를 수행합니다.
///완전 신뢰 모델에서는 재화/아이템 지급을 클라이언트가 로컬 처리 후 saveData로 반영하므로,
///서버는 결제 검증/기록과 서버측에 반드시 남아야 하는 항목(광고 제거 등)만 처리합니다.
async function applyPurchaseReward(docRef, productId){
    switch (productId) {
        //TODO: 실제 Play Console 상품 ID에 맞게 매핑하세요. 아래는 광고 제거 예시입니다.
        case "remove_ads":
            await docRef.update({ removeAds: true });
            break;
        default:
            //그 외 상품은 결제 검증/기록만 담당합니다.
            break;
    }
}
*/

///유저 기본 데이터 스키마를 반환합니다.
///DB 수정이 필요한 경우 데이터를 수정하고 스크립트 상단 VERSION을 1 증가시키고 배포하면 됩니다.
function getDefaultUserData() {
    return {
        schemaVersion: VERSION,
        //1. 광고 제거 여부
        removeAds: false,
        //2. 현재 스테이지
        currentStage: 1,
        //3. 골드
        gold: 0,
        //4. 아이템 개수 (key: 아이템 ID, value: 보유 개수)
        itemCounts: {
            "1005": 1,
            "1006": 1,
            "1007": 1,
            "1008": 1
        },
        //5. 챔피언스 레벨 / 액티브
        championsLevel: 0,
        isChampionsActive: false,
        //리더보드 더미 데이터 (재설치 시 순위 유지용). 첫 접속 시엔 없으며 클라이언트가 saveData로 채웁니다.
        leaderboard: null
    };
}

//====================================================================================
// 레거시 (명세 간략화로 미사용) - 삭제하지 않고 보존. 필요 시 참고/복원용.
//   - getUserDoc            : 구 데이터 읽기(에뮬레이터용) → loadData로 대체됨
//   - progressLogin(구버전) : 버전게이트/스키마 마이그레이션/연속출석 포함 → 간략화됨
//   - startStage / endStage : 서버측 플레이타임 검증/보상계산/리더보드 → 완전신뢰 saveData로 대체
//   - updateAlbumRewardedStage / getItem / progressHousing : 서버측 진행 처리 → 로컬 처리 후 saveData
//   - getLeaderboard        : 리더보드 조회 (명세 4케이스에 미포함)
//   - progressStreakLogin / checkItemCountValid / progressPurchaseProduct_RemoveAds : 레거시 헬퍼
//====================================================================================
/*
///For Test Emulator Only : 유저 데이터를 클라이언트에서 읽어오기 위한 함수입니다.
exports.getUserDoc = onCall(async (request) => {
    const doc = await getDoc(request).get();
    checkDocExists(doc);

    return doc.data();
})

///[레거시 progressLogin] 버전 체크 + 스키마 마이그레이션 + 연속출석 처리 포함 버전
exports.progressLogin = onCall(async (request) => {
    const docRef = getDoc(request);

    const clientVersion = request.data;
    const versionDoc = await db.collection(publicReference).doc("appVersion").get();
    if(clientVersion !== versionDoc.data().version){
        return versionDoc.data().version;
    }

    const doc = await docRef.get();

    //UID DB가 이미 존재하는 경우 버전을 체크하고 마이그레이션합니다.
    if(doc.exists){
        const userData = doc.data();

        if(userData.schemaVersion != VERSION){
            const updateData = {};
            const defaultData = getDefaultUserData();

            //유저의 DB와 최신 DB 스키마를 비교하여 새로 생긴 필드를 추가해줍니다.
            Object.keys(defaultData).forEach(key => {
                if (userData[key] === undefined) {
                    updateData[key] = defaultData[key];
                } else if (typeof defaultData[key] === 'object' && defaultData[key] !== null) {
                    Object.keys(defaultData[key]).forEach(subKey => {
                        if (userData[key][subKey] === undefined) {
                            updateData[key + '.' + subKey] = defaultData[key][subKey];
                        }
                    });
                }
            });
            //유저의 DB와 최신 DB 스키마를 비교하여 없어진 필드를 삭제해줍니다.
            Object.keys(userData).forEach(key => {
                if(defaultData[key] === undefined){
                    updateData[key] = FieldValue.delete();
                }
                else if(userData[key] && typeof userData[key] === 'object' && typeof defaultData[key] === 'object') {
                    Object.keys(userData[key]).forEach(subKey => {
                        if(defaultData[key][subKey] === undefined) {
                            updateData[key + '.' + subKey] = FieldValue.delete();
                        }
                    });
                }
            });

            updateData.schemaVersion = VERSION;

            if(Object.keys(updateData).length > 0){
                await docRef.update(updateData);
                doc = await docRef.get();
            }
        }
        const result = await progressStreakLogin(docRef, doc);
        const data = doc.data();

        data.loginData.maxStreakLoginCount = result;

        return data;
    }
    else{
        await docRef.set(getDefaultUserData());
        return getDefaultUserData();
    }
});

///스테이지 시작 요청 onCall 함수입니다. (플레이 유효성 검증용 시작시간 기록)
exports.startStage = onCall(async (request) =>{
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    if (doc.data().stageData.stageStartTime) {
        throw new HttpsError("already-exists", "이미 스테이지가 시작되었습니다.");
    }
    if(doc.data().isClearLastStage){
        throw new HttpsError("permission-denied", "이미 모든 스테이지를 클리어하였습니다.");
    }

    try {
        await docRef.update({
        "stageData.stageStartTime": FieldValue.serverTimestamp()
    });
    }
    catch (error) {
        throw new HttpsError("not-found", "stageStartTime 데이터가 존재하지 않습니다");
    }
})

///스테이지 종료 요청 onCall 함수입니다. (서버측 플레이타임 검증/보상계산/리더보드 갱신)
exports.endStage = onCall(async (request) =>{
    const uid = getUID(request);
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const isStartTime = doc.data().stageData.stageStartTime;
    if(!isStartTime){
        throw new HttpsError("permission-denied", "비정상적인 요청입니다.");
    }

    const {
        isCleared,
        usedItems,
        nickname = "USER",
        profileImageIndex = 0,
        profileFrameIndex = 0
    } = request.data || {};

    if(!checkItemCountValid(doc, usedItems)){
        await docRef.update({
            "stageStartTime": null
        });
        throw new HttpsError("failed-precondition", "보유한 아이템보다 더 많은 아이템을 사용할 수 없습니다.")
    }

    if(!isCleared){
        await docRef.update({
            [itemPathArr[0]]: FieldValue.increment(-usedItems.blackhole),
            [itemPathArr[1]]: FieldValue.increment(-usedItems.timer),
            [itemPathArr[2]]: FieldValue.increment(-usedItems.bomb),
            "stageData.stageStartTime": null,
            "stageData.isFirstTry": false,
            "stageData.currentStreakStageCount": 0
        });

        return {
            blackhole: doc.data().item.blackhole - usedItems.blackhole,
            timer: doc.data().item.timer - usedItems.timer,
            bomb: doc.data().item.bomb - usedItems.bomb,
            gold: doc.data().currency.gold + (100 * rewardStar),
            star: doc.data().currency.star + rewardStar,
            currentStage: doc.data().stageData.currentStage,
            currentStreakStageCount: doc.data().stageData.currentStreakStageCount,
            maxStreakStageCount: doc.data().stageData.maxStreakStageCount
        }
    }
    else{
        const startTime = doc.data().stageStartTime.toMillis();
        const endTime = Date.now();

        const clearTimeDoc = await db.collection(clearTimePath).doc(clearTimePath).get();
        checkDocExists(clearTimeDoc);

        const remain = doc.data().currentStage % 10;
        const minTime = clearTimeDoc.data()[clearTimeFieldPathArr[remain]];

        const playTime = (endTime - startTime) / 1000;

        if(playTime < minTime){
            await docRef.update({
                "stageData.stageStartTime": null
            });
            throw new HttpsError("permission-denied", "비정상적인 플레이타임입니다.");
        }

        let rewardStar = 0;
        if(playTime < minTime + clearThresholdsArr[0]){
            rewardStar = 3;
        }
        else if(playTime < minTime + clearThresholdsArr[1]){
            rewardStar = 2;
        }
        else{
            rewardStar = 1;
        }

        const stageDoc = await db.collection(publicStageReference).doc("maxStage").get();
        checkDocExists(stageDoc);

        const maxStage = stageDoc.data().maxStage;
        const isLastStage = doc.data().stageData.currentStage + 1 >= maxStage;
        const updateCurrentStage = isLastStage ? doc.data().stageData.currentStage : doc.data().stageData.currentStage + 1;

        const isFirstTry = doc.data().stageData.isFirstTry;
        const updateFirstTryCount = isFirstTry? doc.data().stageData.firstTryCount + 1 : doc.data().stageData.firstTryCount;
        const updateCurrentStreakStageCount = isFirstTry ? doc.data().stageData.currentStreakStageCount + 1 :doc.data().stageData.currentStreakStageCount;

        const isMaxStreakStageCount = updateCurrentStreakStageCount > doc.data().stageData.maxStreakStageCount;
        const updateMaxStreakStageCount = isMaxStreakStageCount? updateCurrentStreakStageCount : doc.data().stageData.maxStreakStageCount;

        await docRef.update({
                [itemPathArr[0]]: FieldValue.increment(-usedItems.blackhole),
                [itemPathArr[1]]: FieldValue.increment(-usedItems.timer),
                [itemPathArr[2]]: FieldValue.increment(-usedItems.bomb),
                "currency.gold": FieldValue.increment(100 * rewardStar),
                "currency.star": FieldValue.increment(rewardStar),
                "stageData.currentStage": updateCurrentStage,
                "stageData.stageStartTime": null,
                "stageData.isFirstTry": true,
                "stageData.firstTryCount": updateFirstTryCount,
                "stageData.currentStreakStageCount": updateCurrentStreakStageCount,
                "stageData.maxStreakStageCount": updateMaxStreakStageCount,
                "stageData.isClearLastStage": isLastStage
        });

        const prevLeaderboardDoc = await db.collection("leaderboard").doc(uid).get();
        const bShouldUpdateTimestamp = !prevLeaderboardDoc.exists ||
            prevLeaderboardDoc.data().currentStage < updateCurrentStage;

        const leaderboardData = {
            nickname,
            profileImageIndex,
            profileFrameIndex,
            currentStage: updateCurrentStage
        };

        if (bShouldUpdateTimestamp) {
            leaderboardData.stageReachedAt = FieldValue.serverTimestamp();
        }

        await db.collection("leaderboard").doc(uid).set(leaderboardData, { merge: true });

        return{
            blackhole: doc.data().item.blackhole - usedItems.blackhole,
            timer: doc.data().item.timer - usedItems.timer,
            bomb: doc.data().item.bomb - usedItems.bomb,
            gold: doc.data().currency.gold + (100 * rewardStar),
            star: doc.data().currency.star + rewardStar,
            currentStage: updateCurrentStage,
            currentStreakStageCount: updateCurrentStreakStageCount,
            maxStreakStageCount: updateMaxStreakStageCount
        };
    }
})

///앨범 보상 수령 완료 단계를 업데이트하는 onCall 함수입니다.
exports.updateAlbumRewardedStage = onCall(async (request) => {
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const { lastAlbumRewardedStage } = request.data;

    const currentValue = doc.data().albumData.lastAlbumRewardedStage || 0;
    if (lastAlbumRewardedStage <= currentValue) {
        return { lastAlbumRewardedStage: currentValue };
    }

    await docRef.update({
        "albumData.lastAlbumRewardedStage": lastAlbumRewardedStage
    });

    return { lastAlbumRewardedStage };
});

//onCall at get item
exports.getItem = onCall(async (request) =>{
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const itemType = request.data;

    await docRef.update({
        [itemPathArr[itemType]]: FieldValue.increment(1)
    });

    const itemNameArr = ["blackhole", "timer", "bomb"];

    return doc.data().item[itemNameArr[itemType]] + 1;
})

///하우징 진행 요청 onCall 함수입니다.
exports.progressHousing = onCall(async (request) =>{
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const star = doc.data().currency.star;
    const chapter = doc.data().housingData.currentChapter;

    const requiredStarDoc = await db.collection(housingStarPath).doc(String(chapter)).get();
    checkDocExists(requiredStarDoc);

    const subChapter = doc.data().housingData.currentSubChapter;
    const requiredStar = requiredStarDoc.data()[String(subChapter)];

    if(star < requiredStar){
        throw new HttpsError("invalid-argument", "별이 부족합니다.");
    }

    const updateValue = {
        star: star - requiredStar,
        currentSubChapter: subChapter,
        currentChapter: chapter
    };

    if(requiredStarDoc.data()[String(subChapter + 1)]  === undefined){

        const housingDoc = await db.collection(publicHousingReference).doc("maxChapter").get();
        const maxChapter = housingDoc.data().maxChapter;

        if(chapter + 1 > maxChapter){
            await docRef.update({
                "currency.star": FieldValue.increment(-requiredStar),
                "housingData.usedStarCount": FieldValue.increment(requiredStar),
                "housingData.isProgressLastChapter": true
            });

            return "end";
        }
        else{
            await docRef.update({
                "currency.star": FieldValue.increment(-requiredStar),
                "housingData.usedStarCount": FieldValue.increment(requiredStar),
                "housingData.currentSubChapter": 1,
                "housingData.currentChapter": FieldValue.increment(1)
            });

            updateValue.currentSubChapter = 1;
            updateValue.currentChapter = chapter + 1;
        }
    }
    else{
        await docRef.update({
            "currency.star": FieldValue.increment(-requiredStar),
             "housingData.usedStarCount": FieldValue.increment(requiredStar),
            "housingData.currentSubChapter": FieldValue.increment(1)
        });

        updateValue.currentSubChapter = subChapter + 1;
    }

    return updateValue;
})

///리더보드 조회 onCall 함수입니다.
exports.getLeaderboard = onCall(async (request) => {
    const uid = getUID(request);
    const n = (request.data && request.data.n) || 100;

    const snapshot = await db.collection("leaderboard")
        .orderBy("currentStage", "desc")
        .orderBy("stageReachedAt", "asc")
        .limit(n)
        .get();

    let rank = 1;
    const topN = [];
    snapshot.forEach(doc => {
        topN.push({
            rank: rank++,
            nickname: doc.data().nickname,
            profileImageIndex: doc.data().profileImageIndex,
            profileFrameIndex: doc.data().profileFrameIndex,
            currentStage: doc.data().currentStage
        });
    });

    const myDoc = await db.collection("leaderboard").doc(uid).get();

    if (!myDoc.exists) {
        return { topN, myEntry: null };
    }

    const myData = myDoc.data();

    const higherSnap = await db.collection("leaderboard")
        .where("currentStage", ">", myData.currentStage)
        .count()
        .get();

    const sameEarlierSnap = await db.collection("leaderboard")
        .where("currentStage", "==", myData.currentStage)
        .where("stageReachedAt", "<", myData.stageReachedAt)
        .count()
        .get();

    const myRank = higherSnap.data().count + sameEarlierSnap.data().count + 1;

    return {
        topN,
        myEntry: {
            rank: myRank,
            nickname: myData.nickname,
            profileImageIndex: myData.profileImageIndex,
            profileFrameIndex: myData.profileFrameIndex,
            currentStage: myData.currentStage
        }
    };
});

///연속 접속 처리 함수 (레거시 헬퍼)
async function progressStreakLogin(docRef, doc){
    checkDocExists(doc);

    const lastLogin = doc.data().loginData.lastLoginDate;
    const lastLoginToDate = lastLogin.toDate();
    const now = new Date();

    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const lastDay = new Date(lastLoginToDate.getFullYear(), lastLoginToDate.getMonth(), lastLoginToDate.getDate());

    const diffMs = today - lastDay;
    const diffDays = diffMs / (1000 * 60 * 60 * 24);

    if(diffDays < 1){
        return doc.data().loginData.maxStreakLoginCount;
    }
    if(diffDays > 1){
        await docRef.update({
            "loginData.maxStreakLoginCount": 1,
            "loginData.lastLoginDate": FieldValue.serverTimestamp()
        });
        return 1;
    }
    await docRef.update({
        "loginData.maxStreakLoginCount": FieldValue.increment(1),
        "loginData.lastLoginDate": FieldValue.serverTimestamp()
    });

    return doc.data().loginData.maxStreakLoginCount + 1;
}

///아이템 개수가 유효한지 체크합니다. (레거시 헬퍼)
function checkItemCountValid(doc, usedItems){
    const items = doc.data().item;

    for (const itemName in usedItems) {
        const useCount = usedItems[itemName];
        const currentCount = items[itemName] || 0;

        if (currentCount < useCount) {
            return false;
        }
    }
    return true;
}

///RemoveAds 상품 구매 함수 (레거시 헬퍼, 미사용)
async function progressPurchaseProduct_RemoveAds(request){
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    await docRef.update({
        removeAdsPurchaseDate: FieldValue.serverTimestamp()
    });
}
*/
