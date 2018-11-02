using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using frame8.ScrollRectItemsAdapter.Util;
using System;

namespace frame8.ScrollRectItemsAdapter.MultiplePrefabsExample
{
    /// <summary>Utility behavior to be attached to a GameObject containing a RawImage for loading remote images using <see cref="SimpleImageDownloader"/>, optionally displaying a specific image during loading and/or on error</summary>
    [RequireComponent(typeof(RawImage))]
    public class RemoteImageBehaviour : MonoBehaviour
    {
        [Tooltip("If not assigned, will try to find it in this game object")]
        [SerializeField] RawImage _RawImage;
#pragma warning disable 0649
        [SerializeField] Texture2D _LoadingTexture;
        [SerializeField] Texture2D _ErrorTexture;
#pragma warning restore 0649

        string _CurrentRequestedURL;
        bool _DestroyPending;
        Texture2D _RecycledTexture;


        void Awake()
        {
            if (!_RawImage)
                _RawImage = GetComponent<RawImage>();
        }

        /// <summary>Starts the loading, setting the current image to <see cref="_LoadingTexture"/>, if available. If the image is already in cache, and <paramref name="loadCachedIfAvailable"/>==true, will load that instead</summary>
        public void Load(string imageURL, bool loadCachedIfAvailable = true, Action<bool, bool> onCompleted = null, Action onCanceled = null)
        {
            // Don't download the same image again
            if (loadCachedIfAvailable && _CurrentRequestedURL == imageURL)
            {
                if (_RecycledTexture)
                {
                    if (_RecycledTexture != _RawImage.texture)
                        _RawImage.texture = _RecycledTexture;

					if (onCompleted != null)
						onCompleted(true, true);

					return;
                }
            }

            if (_RawImage.texture)
            {
                _RecycledTexture = _RawImage.texture as Texture2D;
                if (_RecycledTexture == _LoadingTexture || _RecycledTexture == _ErrorTexture)
                    _RecycledTexture = null;
            }
            else
                _RecycledTexture = null;

            _CurrentRequestedURL = imageURL;
            _RawImage.texture = _LoadingTexture;
            var request = new SimpleImageDownloader.Request()
            {
                url = imageURL,
                onDone = result =>
                {
                    if (!_DestroyPending && imageURL == _CurrentRequestedURL) // this will be false if a new request was done during downloading, case in which the result will be ignored
                    {
                        Texture2D texToUse;
                        if (_RecycledTexture)
                        {
                            result.LoadTextureInto(_RecycledTexture);
                            texToUse = _RecycledTexture;
                        }
                        else
                            texToUse = result.CreateTextureFromReceivedData();

                        _RawImage.texture = texToUse;

						if (onCompleted != null)
							onCompleted(false, true);
					}
					else if (onCanceled != null)
						onCanceled();
				},
                onError = () =>
                {
					if (!_DestroyPending && imageURL == _CurrentRequestedURL) // this will be false if a new request was done during downloading, case in which the result will be ignored
					{
						_RawImage.texture = _ErrorTexture;

						if (onCompleted != null)
							onCompleted(false, false);
					}
					else if (onCanceled != null)
						onCanceled();
				}
            };
            SimpleImageDownloader.Instance.Enqueue(request);
        }

        void OnDestroy()
        {
            _DestroyPending = true;
        }
    }
}
