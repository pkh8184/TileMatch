using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
    /// <summary>
    /// Firebase 관리(C#) DLL이 요구하는 네이티브 라이브러리 버전과,
    /// APK/AAB에 실제로 포함될 네이티브 aar 버전이 일치하는지 검사한다.
    ///
    /// 이 검사가 필요한 이유:
    /// - Assets/Firebase/ 는 .gitignore 대상이라 개발자마다 로컬에 임포트한 SDK 버전이 다를 수 있다.
    ///   (= C#이 '찾는' .so 이름을 결정)
    /// - mainTemplate.gradle 과 AndroidResolverDependencies.xml 은 git으로 공유된다.
    ///   (= APK에 '들어가는' .so 를 결정)
    /// 둘이 어긋나면 빌드는 성공하지만 실기기에서 아래 경고와 함께 Firebase 전체가 죽는다.
    ///   "Firebase's libApp.so was not found for this device's architecture"
    /// </summary>
    public static class FirebaseNativeVersionValidator
    {
        private const string MENU_PATH   = "Build/Firebase 네이티브 버전 검사";

        private const string APP_DLL_PATH   = "Assets/Firebase/Plugins/Firebase.App.dll";
        private const string GRADLE_PATH    = "Assets/Plugins/Android/mainTemplate.gradle";
        private const string RESOLVER_PATH  = "ProjectSettings/AndroidResolverDependencies.xml";
        private const string LOCAL_REPO_FMT = "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/firebase/firebase-app-unity/{0}/firebase-app-unity-{0}.aar";

        //Firebase.App.dll 안에 박혀 있는 네이티브 라이브러리 이름. 예) FirebaseCppApp-13_8_0
        private static readonly Regex NATIVE_NAME_REGEX = new Regex(@"FirebaseCppApp-(\d+)_(\d+)_(\d+)");

        //gradle / resolver 양쪽에서 firebase-*-unity 패키지와 버전을 뽑는다.
        private static readonly Regex UNITY_PACKAGE_REGEX =
            new Regex(@"com\.google\.firebase:(firebase-[a-z]+-unity):(\d+\.\d+\.\d+)");

        /// <summary>
        /// 검사를 수행한다. 문제가 없으면 true.
        /// message에는 성공/실패 사유가 담기며, 실패 시 그대로 사용자에게 보여주면 된다.
        /// </summary>
        public static bool Validate(out string message)
        {
            //1. C# 쪽이 요구하는 네이티브 버전
            if(!TryReadRequiredNativeVersion(out string requiredVersion, out string readError))
            {
                message = readError;
                return false;
            }

            //2. APK에 실제로 들어갈 버전 (공유 파일 두 곳)
            List<string> problems = new List<string>();

            CollectMismatches(GRADLE_PATH, "mainTemplate.gradle", requiredVersion, problems);
            CollectMismatches(RESOLVER_PATH, "AndroidResolverDependencies.xml", requiredVersion, problems);

            if(problems.Count > 0)
            {
                message = BuildFailureMessage(requiredVersion, problems);
                return false;
            }

            //3. 참고 정보: 리졸브된 aar이 로컬에 있는지 (없으면 Gradle이 원격에서 받아오므로 실패는 아니다)
            string aarPath = string.Format(LOCAL_REPO_FMT, requiredVersion);
            string aarNote = File.Exists(aarPath)
                ? "로컬 aar 확인됨"
                : "로컬 aar 없음(Gradle이 원격에서 받아옴 - 정상일 수 있음)";

            message = $"Firebase 네이티브 버전 일치: {requiredVersion} ({aarNote})";
            return true;
        }

        [MenuItem(MENU_PATH)]
        private static void ValidateFromMenu()
        {
            bool bIsValid = Validate(out string message);

            EditorUtility.DisplayDialog(
                bIsValid ? "Firebase 버전 검사 통과" : "Firebase 버전 불일치",
                message,
                "확인");

            if(bIsValid)
            {
                Debug.Log($"[FirebaseVersionValidator] {message}");
            }
            else
            {
                Debug.LogError($"[FirebaseVersionValidator]\n{message}");
            }
        }

        //Firebase.App.dll에 박힌 네이티브 라이브러리 이름에서 버전을 뽑는다. (13_8_0 → 13.8.0)
        private static bool TryReadRequiredNativeVersion(out string version, out string error)
        {
            version = null;

            if(!File.Exists(APP_DLL_PATH))
            {
                error = $"{APP_DLL_PATH} 가 없습니다.\nFirebase Unity SDK가 임포트되지 않았습니다.";
                return false;
            }

            byte[] bytes = File.ReadAllBytes(APP_DLL_PATH);

            //DLL 안의 문자열은 인코딩이 섞여 있어 ASCII → UTF-16 순으로 훑는다.
            Match match = NATIVE_NAME_REGEX.Match(Encoding.ASCII.GetString(bytes));
            if(!match.Success)
            {
                match = NATIVE_NAME_REGEX.Match(Encoding.Unicode.GetString(bytes));
            }

            if(!match.Success)
            {
                error = $"{APP_DLL_PATH} 에서 네이티브 라이브러리 이름(FirebaseCppApp-x_y_z)을 찾지 못했습니다.";
                return false;
            }

            version = $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";
            error   = null;
            return true;
        }

        //파일 안의 모든 firebase-*-unity 항목을 요구 버전과 대조해 어긋난 것만 모은다.
        private static void CollectMismatches(string filePath, string displayName, string requiredVersion, List<string> problems)
        {
            if(!File.Exists(filePath))
            {
                problems.Add($"[{displayName}] 파일이 없습니다: {filePath}");
                return;
            }

            MatchCollection matches = UNITY_PACKAGE_REGEX.Matches(File.ReadAllText(filePath));

            if(matches.Count == 0)
            {
                problems.Add($"[{displayName}] firebase-*-unity 의존성이 하나도 없습니다. Force Resolve가 필요합니다.");
                return;
            }

            //같은 패키지가 여러 번 나올 수 있으므로 중복 보고를 막는다.
            HashSet<string> reported = new HashSet<string>();

            foreach(Match match in matches)
            {
                string packageName = match.Groups[1].Value;
                string packageVersion = match.Groups[2].Value;

                if(packageVersion == requiredVersion || !reported.Add($"{packageName}:{packageVersion}"))
                {
                    continue;
                }

                problems.Add($"[{displayName}] {packageName} → {packageVersion} (요구: {requiredVersion})");
            }
        }

        private static string BuildFailureMessage(string requiredVersion, List<string> problems)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("C# DLL이 요구하는 네이티브 버전과 APK에 포함될 버전이 다릅니다.");
            builder.AppendLine("이대로 빌드하면 실기기에서 Firebase 전체가 동작하지 않습니다.");
            builder.AppendLine("(\"libApp.so was not found for this device's architecture\")");
            builder.AppendLine();
            builder.AppendLine($"로컬 SDK 요구 버전 : {requiredVersion}");
            builder.AppendLine();
            builder.AppendLine("불일치 항목:");

            foreach(string problem in problems)
            {
                builder.AppendLine($"  - {problem}");
            }

            builder.AppendLine();
            builder.AppendLine("해결: Assets → External Dependency Manager → Android Resolver → Force Resolve");
            builder.AppendLine("실행 후 변경된 gradle/resolver 파일을 커밋하세요.");
            builder.AppendLine("(팀원과 Firebase SDK 버전이 다르면 먼저 버전을 통일해야 합니다)");

            return builder.ToString();
        }
    }
}
