using System;
using UnityEngine;
using GoogleMobileAds.Api;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public class AdManager
    {
        private static AdManager s_Instance;
        public static AdManager Instance => s_Instance ?? (s_Instance = new AdManager());

        private RewardedAd m_RewardedAd;
        // Test ID for Rewarded Ad (Android)
        private string m_AdUnitId = "ca-app-pub-3940256099942544/5224354917"; 

        public void Init()
        {
            MobileAds.Initialize(initStatus => {
                Log.Info("Google Mobile Ads Initialized.");
                LoadRewardedAd();
            });
        }

        public void LoadRewardedAd()
        {
            // Clean up the old ad before loading a new one.
            if (m_RewardedAd != null)
            {
                m_RewardedAd.Destroy();
                m_RewardedAd = null;
            }

            Log.Info("Loading the rewarded ad.");

            var adRequest = new AdRequest();

            RewardedAd.Load(m_AdUnitId, adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    // if error is not null, the load request failed.
                    if (error != null || ad == null)
                    {
                        Log.Error("Rewarded ad failed to load an ad with error : "
                                  + error);
                        return;
                    }

                    Log.Info("Rewarded ad loaded with response : "
                             + ad.GetResponseInfo());

                    m_RewardedAd = ad;
                    RegisterEventHandlers(m_RewardedAd);
                });
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Log.Info(String.Format("Rewarded ad paid {0} {1}.",
                    adValue.Value,
                    adValue.CurrencyCode));
            };
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += () =>
            {
                Log.Info("Rewarded ad recorded an impression.");
            };
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                Log.Info("Rewarded ad was clicked.");
            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                Log.Info("Rewarded ad full screen content opened.");
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                Log.Info("Rewarded ad full screen content closed.");
                // Reload the ad so that we can show another one as soon as possible.
                LoadRewardedAd();
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Log.Error("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
                // Reload the ad so that we can show another one as soon as possible.
                LoadRewardedAd();
            };
        }

        public void ShowRewardedAd(Action onReward)
        {
            if (m_RewardedAd != null && m_RewardedAd.CanShowAd())
            {
                m_RewardedAd.Show((Reward reward) =>
                {
                    Log.Info(String.Format("Rewarded ad rewarded the user. Type: {0}, Amount: {1}",
                        reward.Type,
                        reward.Amount));
                    
                    onReward?.Invoke();
                });
            }
            else
            {
                Log.Warning("Rewarded ad is not ready yet.");
            }
        }
    }
}
