using System.Collections;
using System.Collections.Generic;
using TrumpTile.FrameLibrary;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrumpTile.GameMain.UI
{
    public class SceneTransister : Singleton_GameObject<SceneTransister>
    {
        [Header("씬 전환 오브젝트")]
        [SerializeField] private GameObject mSceneTransister;
        [SerializeField] private Animator mTransisterAnimator;
        private string targetSceneName;

        private bool mbSceneTransitionProgressing = false;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        public void PlayFadeInAnim()
        {
            StartCoroutine(Co_FadeInAnim());
        }
        public IEnumerator Co_PlayFadeInAnim()
        {
            yield return StartCoroutine(Co_FadeInAnim());
        }
        public void TransistScene(string sceneName)
        {
            if (mbSceneTransitionProgressing) return;

            mbSceneTransitionProgressing = true;

            targetSceneName = sceneName;

            StartCoroutine(Co_SecneTransitionProgress());
        }
        private IEnumerator Co_SecneTransitionProgress()
        {
            yield return StartCoroutine(Co_FadeOutAnim());

            AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                if (op.progress >= 0.9f)
                {
                    break;
                }
                yield return null;
            }
            Debug.Log("[IngameView] 메인 씬 로딩 성공");

            op.allowSceneActivation = true;

            mbSceneTransitionProgressing = false;
        }
        private IEnumerator Co_FadeInAnim()
        {
            if (mSceneTransister == null || mTransisterAnimator == null) yield break;

            mSceneTransister.SetActive(true);
            mTransisterAnimator.SetTrigger("FadeIn");

            yield return new WaitForSecondsRealtime(1f);

            mSceneTransister.SetActive(false);
        }
        private IEnumerator Co_FadeOutAnim()
        {
            if (mSceneTransister == null || mTransisterAnimator == null) yield break;

            mSceneTransister.SetActive(true);
            mTransisterAnimator.SetTrigger("FadeOut");

            yield return new WaitForSecondsRealtime(1f);
        }
    }
}
