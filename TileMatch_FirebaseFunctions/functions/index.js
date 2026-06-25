//#region init
//사용할 파이어베이스 라이브러리 기본 설정
const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { setGlobalOptions } = require("firebase-functions/v2");
const { getFirestore, FieldValue, Timestamp } = require("firebase-admin/firestore");
const admin = require("firebase-admin");
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

/*must write in onCall top (except login) : 로그인을 제외한 문서를 수정하는 모든 onCall 함수에서 문서의 유효성을 체크합니다. 
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);
*/

//#region variable
//Functions 버전입니다.
const VERSION = 2;

//모든 유저가 서버 검증을 위해 공용으로 참조할 collection의 경로명입니다. 
const clearTimePath = "minTimeRequiredClearStage";
const housingStarPath = "starRequiredProgressHousing";
const publicStageReference = "publicStageReference";
const publicHousingReference = "publicHousingReference";
const publicReference = "publicReference";

//array
const itemPathArr = ["item.blackhole", "item.timer", "item.bomb"];
const clearTimeFieldPathArr = ["normal","normal","normal","normal","normal","normal","normal","normal","normal","normal"];
const clearThresholdsArr = [30, 50];
//#endregion

///For Test Emulator Only : 유저 데이터를 클라이언트에서 읽어오기 위한 함수입니다. 에뮬레이터 테스트 환경에서만 사용합니다. 배포하지 않습니다.
exports.getUserDoc = onCall(async (request) => {
    const doc = await getDoc(request).get();
    checkDocExists(doc);

    return doc.data();
})
 
//로컬 UI 데이터를 갱신하는 함수의 경우, 모든 경우에서 갱신될 수 있는 모든 데이터값을 반환합니다.
//ex) 스테이지 클리어 실패 시 currentStage값은 갱신되지 않지만, 현재값으로 반환 -> 요청에 따른 반환형을 통일하여 일괄적으로 처리할 수 있게
//#region 아래부터 onCall 함수입니다. 각 함수는 클라이언트에서 호출되는 플로우에 따라 순서대로 작성되었습니다.

///로그인 요청 onCall 함수입니다.
///접속한 유저의 데이터 버전을 체크한 후, 버전에 맞게 DB를 마이그레이션합니다.
///처음 접속한 유저의 경우 DB를 새로 생성합니다.
///이후 연속 접속을 처리하는 progressStreakLogin 함수를 호출합니다.
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
            //중첩 필드까지 검사하여 처리합니다.
            Object.keys(defaultData).forEach(key => {
                if (userData[key] === undefined) {
                    updateData[key] = defaultData[key];
                } else if (typeof defaultData[key] === 'object' && defaultData[key] !== null) {
                    Object.keys(defaultData[key]).forEach(subKey => {
                        if (userData[key][subKey] === undefined) {
                            updateData[`${key}.${subKey}`] = defaultData[key][subKey];
                        }
                    });
                }
            });
            //유저의 DB와 최신 DB 스키마를 비교하여 없어진 필드를 삭제해줍니다.
            //중첩 필드까지 검사하여 처리합니다.
            Object.keys(userData).forEach(key => {
                if(defaultData[key] === undefined){
                    updateData[key] = FieldValue.delete();
                }
                else if(userData[key] && typeof userData[key] === 'object' && typeof defaultData[key] === 'object') {
                    Object.keys(userData[key]).forEach(subKey => {
                        if(defaultData[key][subKey] === undefined) {
                            updateData[`${key}.${subKey}`] = FieldValue.delete();
                        }
                    });
                }
            });

            updateData.schemaVersion = VERSION;

            //업데이트할 데이터가 있는 경우에만 업데이트를 호출합니다.
            if(Object.keys(updateData).length > 0){
                await docRef.update(updateData);

                //데이터를 업데이트한 후 최신화 된 문서를 읽어옵니다.
                doc = await docRef.get();
            }
        }
        const result = await progressStreakLogin(docRef, doc);
        const data = doc.data();

        data.loginData.maxStreakLoginCount = result;

        return data;
    }
    else{
        //첫 접속시에만 호출됩니다.
        await docRef.set(getDefaultUserData());

        return getDefaultUserData();
    }
});

///스테이지 시작 요청 onCall 함수입니다.
///플레이 유효성을 검사하기 위해 스테이지 시작 시간을 저장합니다.
///이미 스테이지를 시작한 상태에서 또 스테이지를 시작하려는 경우를 방지합니다.
exports.startStage = onCall(async (request) =>{
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    //이미 스테이지를 시작한 경우 예외를 던집니다.
    if (doc.data().stageData.stageStartTime) {
        throw new HttpsError("already-exists", "이미 스테이지가 시작되었습니다.");
    }
    //마지막 스테이지를 이미 클리어한 경우 예외를 던집니다.
    if(doc.data().isClearLastStage){
        throw new HttpsError("permission-denied", "이미 모든 스테이지를 클리어하였습니다.");
    }

    //stageStartTime 필드를 현재 시간으로 갱신합니다.
    try {
        await docRef.update({
        "stageData.stageStartTime": FieldValue.serverTimestamp()
    });
    }
    catch (error) {
        throw new HttpsError("not-found", "stageStartTime 데이터가 존재하지 않습니다");
    }
})

///스테이지 종료 요청 onCall 함수입니다.
///플레이 데이터를 클라이언트에게 넘겨받습니다.
///플레이 데이터가 유효한지 검사하고, 플레이 데이터에 맞는 처리를 해줍니다.
///스테이지를 클리어한 경우, 스테이지 클리어 기록 관련 데이터들을 갱신해줍니다.
//(03/09 : 플레이 데이터 검증 어떻게 할 건지 정한 후 수정해야함)
exports.endStage = onCall(async (request) =>{
    const uid = getUID(request);          // 추가: uid 추출
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const isStartTime = doc.data().stageData.stageStartTime;
    if(!isStartTime){
        throw new HttpsError("permission-denied", "비정상적인 요청입니다.");
    }

    // 변경: request.data null 안전성 + profile 필드 추가
    const {
        isCleared,
        usedItems,
        nickname = "USER",
        profileImageIndex = 0,
        profileFrameIndex = 0
    } = request.data || {}; 

    //사용한 아이템의 개수가 보유 아이템 개수보다 많은 경우 플레이를 무효 처리합니다.
    if(!checkItemCountValid(doc, usedItems)){
        await docRef.update({
            "stageStartTime": null
        });
        throw new HttpsError("failed-precondition", "보유한 아이템보다 더 많은 아이템을 사용할 수 없습니다.")
    }

    //스테이지를 클리어하지 않은 경우에도 사용한 아이템들의 개수를 갱신하고 스테이지 시작 시간을 초기화합니다.
    //스테이지 첫 시도 체크를 false로 해줍니다.
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
    //스테이지를 클리어한 경우 정상적인 방법으로 클리어했는지 검증합니다.
    //검증에 통과한 경우 플레이 데이터에 기반하여 현재 스테이지의 요구사항에 맞는 보상을 지급합니다.
    //사용한 아이템들의 개수를 갱신하고 스테이지 시작 시간을 초기화합니다.
    else{ 

//#region 유효한 플레이 검증 어떻게 할건지 정해야함
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
//#endregion
        
        //현재 버전의 마지막 스테이지를 클리어한건지 체크합니다.
        const stageDoc = await db.collection(publicStageReference).doc("maxStage").get();
        checkDocExists(stageDoc);

        //마지막 스테이지 클리어인지 여부에 따라 isLastStage와 currentStage의 값을 지정해줍니다.
        const maxStage = stageDoc.data().maxStage;
        const isLastStage = doc.data().stageData.currentStage + 1 >= maxStage;
        const updateCurrentStage = isLastStage ? doc.data().stageData.currentStage : doc.data().stageData.currentStage + 1;
        
        //첫 시도에 클리어했는지 여부에 따라 현재 연속 첫 시도 스테이지 클리어 횟수와 첫 시도에 클리어한 스테이지 개수의 값을 지정해줍니다. 
        const isFirstTry = doc.data().stageData.isFirstTry;
        const updateFirstTryCount = isFirstTry? doc.data().stageData.firstTryCount + 1 : doc.data().stageData.firstTryCount;
        const updateCurrentStreakStageCount = isFirstTry ? doc.data().stageData.currentStreakStageCount + 1 :doc.data().stageData.currentStreakStageCount;

        //현재 연속 첫 시도에 스테이지 클리어 횟수가 최고 연속 첫 시도에 스테이지 클리어 횟수보다 높은 경우 최고 기록 값을 지정해줍니다.
        const isMaxStreakStageCount = updateCurrentStreakStageCount > doc.data().stageData.maxStreakStageCount;
        const updateMaxStreakStageCount = isMaxStreakStageCount? updateCurrentStreakStageCount : doc.data().stageData.maxStreakStageCount;
        
        //지정된 값들로 데이터들을 갱신해줍니다.
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

        // leaderboard 업데이트
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

        //로컬 UI에 표시되는 데이터들의 갱신을 위해 데이터를 클라이언트에게 반환합니다.
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
///클라이언트에서 앨범 보상 팝업이 완료된 후 호출합니다.
///lastAlbumRewardedStage 값을 갱신하여 다음 세션에서 중복 보상을 방지합니다.
exports.updateAlbumRewardedStage = onCall(async (request) => {
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const { lastAlbumRewardedStage } = request.data;

    const currentValue = doc.data().albumData?.lastAlbumRewardedStage ?? 0;
    if (lastAlbumRewardedStage <= currentValue) {
        return { lastAlbumRewardedStage: currentValue };
    }

    await docRef.update({
        "albumData.lastAlbumRewardedStage": lastAlbumRewardedStage
    });

    return { lastAlbumRewardedStage };
});

///상품 구매 요청 onCall 함수입니다.
///구글 영수증 토큰과 상품 넘버를 클라이언트에게 넘겨받습니다.
///영수증 유효성을 검사하여 비정상적인 구매 요청을 방지합니다.
///올바른 구매 요청인 경우 상품 넘버에 맞는 요소를 증가시킵니다.
exports.purchaseProduct = onCall(async (request) =>{
    
})

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
///현재 챕터의 현재 서브챕터에서 진행 시 요구되는 별 개수와 유저의 별 개수를 비교합니다.
///요구 별 개수가 충족되는 경우 서브챕터를 진행합니다.
///현재 서브챕터가 마지막인 경우 챕터를 1 증가시키고 서브챕터를 1로 초기화합니다.
exports.progressHousing = onCall(async (request) =>{
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    const star = doc.data().currency.star;
    const chapter = doc.data().housingData.currentChapter;

    //챕터별 필요한 별 컬렉션에서 유저의 현재 챕터에 해당하는 문서를 읽어옵니다.
    const requiredStarDoc = await db.collection(housingStarPath).doc(String(chapter)).get();
    checkDocExists(requiredStarDoc);

    //챕터별 필요한 별 문서에서 유저의 현재 서브챕터에 맞는 필드를 읽어옵니다.
    const subChapter = doc.data().housingData.currentSubChapter;
    const requiredStar = requiredStarDoc.data()[String(subChapter)];

    //유저가 보유한 별이 요구되는 별보다 적은 경우를 방지합니다.
    if(star < requiredStar){
        throw new HttpsError("invalid-argument", "별이 부족합니다.");
    }

    //클라이언트에 반환할 값입니다. 하우징 관련 로컬 UI에 표시할 모든 데이터를 반환합니다.
    const updateValue = {
        star: star - requiredStar,
        currentSubChapter: subChapter,
        currentChapter: chapter
    };

    //현재 서브챕터가 마지막 서브챕터인 경우 현재 챕터가 마지막 챕터인지 검사합니다.
    if(requiredStarDoc.data()[String(subChapter + 1)]  === undefined){
        
        const housingDoc = await db.collection(publicHousingReference).doc("maxChapter").get();
        const maxChapter = housingDoc.data().maxChapter;

        //현재 챕터가 마지막 챕터인 경우 isProgressLastChapter를 true로 해줍니다.
        if(chapter + 1 > maxChapter){
            await docRef.update({
                "currency.star": FieldValue.increment(-requiredStar),
                "housingData.usedStarCount": FieldValue.increment(requiredStar),
                "housingData.isProgressLastChapter": true
            });

            //클라이언트에서의 처리를 위해 마지막 챕터까지 완료했다는 사실을 반환합니다.
            return "end";
        }
        //현재 챕터가 마지막 챕터가 아닌 경우 currentSubChapter를 1로 초기화하고 currentChapter를 1 증가시킵니다.
        else{
            await docRef.update({
                "currency.star": FieldValue.increment(-requiredStar),
                "housingData.usedStarCount": FieldValue.increment(requiredStar),
                "housingData.currentSubChapter": 1,
                "housingData.currentChapter": FieldValue.increment(1)
            });

            //클라이언트에게 반환할 데이터를 알맞게 수정합니다.
            updateValue.currentSubChapter = 1;
            updateValue.currentChapter = chapter + 1;
        }
    }
    //현재 서브챕터가 마지막이 아닌 경우 서브챕터를 1 증가시킵니다.
    else{
        await docRef.update({
            "currency.star": FieldValue.increment(-requiredStar),
             "housingData.usedStarCount": FieldValue.increment(requiredStar),
            "housingData.currentSubChapter": FieldValue.increment(1)
        });

         //클라이언트에게 반환할 데이터를 알맞게 수정합니다.
        updateValue.currentSubChapter = subChapter + 1;
    }

    return updateValue;
})
//#endregion

////-------------아래부터는 클라이언트 호출 함수가 아닙니다.------------------

//#region onCall 함수에서 호출하는 함수

///서버 함수를 호출한 유저의 UID를 반환합니다.
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

///문서가 존재하는지 체크합니다.
function checkDocExists(doc){
    if(!doc.exists)
    {
        throw new HttpsError("not-found", "데이터가 존재하지 않습니다.");
    }
}

///아이템 개수가 유효한지 체크합니다. 유효하지 않은 경우 false를 반환합니다.
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

///유저 기본 데이터 스키마를 반환합니다.
///DB 수정이 필요한 경우 데이터를 수정하고 스크립트 상단 VERSION을 1 증가시키고 배포하면 됩니다.
function getDefaultUserData() {
    return {
        schemaVersion: VERSION,
        stageData: {
            currentStage: 1,
            firstTryCount: 0,
            maxStreakStageCount: 0,
            currentStreakStageCount: 0,
            isFirstTry: true,
            isClearLastStage: false
        },
        currency: {
            gold: 0,
            star: 0
        },
        item: {
            blackhole: 0,
            timer: 0,
            bomb: 0
        },
        removeAdsPurchaseDate: null,
        housingData: {
            currentChapter: 1,
            currentSubChapter: 1,
            usedStarCount: 0,
            completedChapterCount: 0,
            isProgressLastChapter: false
        },
        albumData: {
            lastAlbumRewardedStage: 0
        },
        loginData: {
            firstLoginDate: FieldValue.serverTimestamp(),
            maxStreakLoginCount: 0,
            lastLoginDate: FieldValue.serverTimestamp()
        }
    };
}
//#endregion

//#region onCall 함수에서 호출하는 비동기 함수

///연속 접속 처리 함수
///연속 접속 여부를 체크하여 처리합니다.
///로그인 시 호출되며, 문서 업데이트가 일어났을 수 있기 때문에 해당 함수 호출 시 문서를 다시 읽어옵니다.
async function progressStreakLogin(docRef, doc){
    checkDocExists(doc);

    //마지막 접속 날짜와 현재 날짜를 불러와 며칠이 지났는지 체크합니다.
    const lastLogin = doc.data().loginData.lastLoginDate;
    const lastLoginToDate = lastLogin.toDate();
    const now = new Date();

    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const lastDay = new Date(lastLoginToDate.getFullYear(), lastLoginToDate.getMonth(), lastLoginToDate.getDate());

    const diffMs = today - lastDay;
    const diffDays = diffMs / (1000 * 60 * 60 * 24);

    //같은 날에 접속한 경우 아무런 처리도 하지 않습니다.
    if(diffDays < 1){
        return doc.data().loginData.maxStreakLoginCount;
    }
    //연속으로 접속하지 않은 경우(이틀 이상 지난 후 접속한 경우) 최대 로그인 기록을 초기화합니다.
    if(diffDays > 1){
        await docRef.update({
            "loginData.maxStreakLoginCount": 1,
            "loginData.lastLoginDate": FieldValue.serverTimestamp()
        });
        return 1;
    }
    //연속으로 접속한 경우 최대 로그인 기록을 1 증가시킵니다.
    await docRef.update({
        "loginData.maxStreakLoginCount": FieldValue.increment(1),
        "loginData.lastLoginDate": FieldValue.serverTimestamp()
    });

    return doc.data().loginData.maxStreakLoginCount + 1;
}

///RemoveAds 상품 구매 함수
///removeAdsPurchaseDate를 현재 서버 시간으로 설정합니다.
async function progressPurchaseProduct_RemoveAds(request){
    const docRef = getDoc(request);
    const doc = await docRef.get();
    checkDocExists(doc);

    await docRef.update({
        removeAdsPurchaseDate: FieldValue.serverTimestamp()
    });
}
//#endregion
