using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrumpTile.GameMain.UI
{
    public class SceneTransister : Singleton_GameObject<SceneTransister>
    {
        [Header("씬 전환 오브젝트")]
        [SerializeField] private GameObject mSceneTransister;
        [SerializeField] private RectTransform[] mRectArray;
        
        private Vector2[] mOriginPosArray;
        private Quaternion[] mOriginRotationArray;
        private string targetSceneName;

        private bool mbSceneTransitionProgressing = false;
        private void Awake()
        {
            if (FindObjectsOfType<SceneTransister>().Length > 1)
			{
				Destroy(gameObject);
				return;
			}
			DontDestroyOnLoad(gameObject);

            mOriginPosArray = new Vector2[mRectArray.Length];
            mOriginRotationArray = new Quaternion[mRectArray.Length];

            for(int i = 0; i < mOriginPosArray.Length; i++)
            {
                mOriginPosArray[i] = mRectArray[i].anchoredPosition;
                mOriginRotationArray[i] = mRectArray[i].localRotation;
            }
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
            EAudioKey key = targetSceneName == "MainScene"? EAudioKey.BGM_Main : EAudioKey.BGM_Ingame;
            AudioEvent.Play(key);

            mbSceneTransitionProgressing = false;
        }
        private void SetFadeOutValid()
        {
            Vector3 direction = Vector3.zero;

            mRectArray[0].anchoredPosition = mOriginPosArray[0];
            mRectArray[0].rotation =  mOriginRotationArray[0];

            if(mRectArray[0].localPosition.y < 0)
            {
                direction = mRectArray[0].transform.up * -1;
            }
            else
            {
                direction = mRectArray[0].transform.up;
            }

            Vector2 targetPos = mRectArray[0].anchoredPosition + (Vector2)direction * 1920f;
            mRectArray[0].anchoredPosition = targetPos;

            float randomAngle = Random.Range(30f, 60f);
            float randomDir = Random.value > 0.5f ? 1f : -1f;
            mRectArray[0].rotation = Quaternion.Euler(new Vector3(0, 0, randomAngle * randomDir));

            for(int i = 1; i < mRectArray.Length; i++)
            {
                mRectArray[i].anchoredPosition = mOriginPosArray[i];
                mRectArray[i].rotation = mOriginRotationArray[i];
                Vector2 _direction = mRectArray[i].anchoredPosition.normalized;
                Vector2 _targetPos = mRectArray[i].anchoredPosition + _direction * 1000f;
                
                mRectArray[i].anchoredPosition = _targetPos;

                randomAngle = Random.Range(30f, 60f);
                randomDir = Random.value > 0.5f ? 1f : -1f;
                mRectArray[i].rotation = Quaternion.Euler(new Vector3(0, 0, randomAngle * randomDir));
            }
        }
        private IEnumerator Co_FadeInAnim()
        {
            if (mSceneTransister == null) yield break;

            mSceneTransister.SetActive(true);

            Sequence seq = DOTween.Sequence();
            Vector3 direction = Vector3.zero;
            
            mRectArray[0].anchoredPosition = mOriginPosArray[0];
            mRectArray[0].rotation =  mOriginRotationArray[0];

            if(mRectArray[0].localPosition.y < 0)
            {
                direction = mRectArray[0].transform.up * -1;
            }
            else
            {
                direction = mRectArray[0].transform.up;
            }

            Vector2 targetPos = mRectArray[0].anchoredPosition + (Vector2)direction * 1920f;
            seq.Append(mRectArray[0].DOAnchorPos(targetPos, 1f));

            float randomAngle = Random.Range(30f, 60f);
            float randomDir = Random.value > 0.5f ? 1f : -1f;
            seq.Join(mRectArray[0].DORotate(new Vector3(0, 0, randomAngle * randomDir), 1f, RotateMode.LocalAxisAdd));

            for(int i = 1; i < mRectArray.Length; i++)
            {
                mRectArray[i].anchoredPosition = mOriginPosArray[i];
                mRectArray[i].rotation = mOriginRotationArray[i];
                Vector2 _direction = mRectArray[i].anchoredPosition.normalized;
                Vector2 _targetPos = mRectArray[i].anchoredPosition + _direction * 1000f;
                
                seq.Insert(0.01f * i, mRectArray[i].DOAnchorPos(_targetPos, 1f));

                randomAngle = Random.Range(30f, 60f);
                randomDir = Random.value > 0.5f ? 1f : -1f;
                seq.Join(mRectArray[i].DORotate(new Vector3(0, 0, randomAngle * randomDir), 1f, RotateMode.LocalAxisAdd));
            }
            AudioEvent.Play(EAudioKey.SFX_SceneTransition_Out);

            yield return seq.WaitForCompletion(); 

            mSceneTransister.SetActive(false);
        }
        private IEnumerator Co_FadeOutAnim()
        {
            if (mSceneTransister == null) yield break;

            SetFadeOutValid();

            mSceneTransister.SetActive(true);

            Sequence seq = DOTween.Sequence();

            seq.Append(mRectArray[0].DOAnchorPos(mOriginPosArray[0], 1f));
            seq.Join(mRectArray[0].DORotate(new Vector3(0,0,mOriginRotationArray[0].eulerAngles.z), 1f));

            for(int i = 1; i < mRectArray.Length; i++)
            {      
                seq.Insert(0.01f * i, mRectArray[i].DOAnchorPos(mOriginPosArray[i], 1f));
                seq.Join(mRectArray[i].DORotate(new Vector3(0,0,mOriginRotationArray[i].eulerAngles.z), 1f));
            }
            AudioEvent.Play(EAudioKey.SFX_SceneTransition_Out);

            yield return seq.WaitForCompletion();
        }
    }
}
