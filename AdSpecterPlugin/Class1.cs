﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;


namespace AdSpecter
{
    [Serializable]
    public class DeveloperApp
    {
        public int id;
        public string name;
        public int developer_app_id;
        public User user;

        public DeveloperApp(int developerAppId)
        {
            developer_app_id = developerAppId;
        }

        public static DeveloperApp CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<DeveloperApp>(jsonString);
        }

        public string SaveToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class User
    {
        public int id;
        public string first_name;
        public string last_name;
        public string full_name;
        public string account_type;
        public string username;
        public string email;
        public string authentication_token;

        public static User CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<User>(jsonString);
        }
    }

    [Serializable]
    public class AdUnitData
    {
        public int id;
        public string title;
        public string description;
        public string click_url;
        public string ad_unit_url;
        public bool active;
        public User user;

        public static AdUnitData CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<AdUnitData>(jsonString);
        }
    }

    [Serializable]
    public class AdUnitDataWrapper
    {
        public AdUnitData ad_unit_data;

        public static AdUnitDataWrapper CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<AdUnitDataWrapper>(jsonString);
        }
    }

    [Serializable]
    public class Impression
    {
        public int id;
        public int ad_unit_id;
        public int developer_app_id;
        public int app_session_id;
        public bool served;
        public bool clicked;
        public bool shown;

        public Impression(int adUnitId, int developerAppId, int appSessionId)
        {
            id = 0;
            served = true;
            clicked = false;
            shown = false;
            ad_unit_id = adUnitId;
            developer_app_id = developerAppId;
            app_session_id = appSessionId;
        }

        public static Impression CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<Impression>(jsonString);
        }
    }

    [Serializable]
    public class ImpressionWrapper
    {
        public Impression impression;

        public static ImpressionWrapper CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<ImpressionWrapper>(jsonString);
        }

        public string SaveToString()
        {
            return JsonUtility.ToJson(this);
        }
    }




    [Serializable]
    public class AppSession
    {
        public int id;
        public int developer_app_id;

        public static AppSession CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<AppSession>(jsonString);
        }
    }

    [Serializable]
    public class AppSessionWrapper
    {
        public AppSession app_session;

        public static AppSessionWrapper CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<AppSessionWrapper>(jsonString);
        }
    }

    [Serializable]
    public class Device
    {
        public string device_model;

        public Device()
        {
            device_model = SystemInfo.deviceModel;

        }

        public static Device CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<Device>(jsonString);
        }

        public string SaveToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class AppSetup
    {
        public string developer_key;
        public Device device;

        public AppSetup(string developerKey)
        {
            developer_key = developerKey;
        }

        public string SaveToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class AppSetupWrapper
    {
        public AppSetup developer_app;
   
        public string SaveToString()
        {
            return JsonUtility.ToJson(this);
        }
    }


    public class AdLoaderPlugIn : MonoBehaviour
    {
        private GameObject ASRUAdUnit;

        private AdUnitDataWrapper adUnitDataWrapper;
        private ImpressionWrapper impressionWrapper;
        public bool startUpdate;

        void Start()
        {
            startUpdate = false;
        }

        public IEnumerator GetAdUnit(GameObject Unit, string format)
        {
            //format must be "image" or "video"
            ASRUAdUnit = Unit;
            //JSON in here
            UnityWebRequest uwr = UnityWebRequest.Get("https://adspecter-sandbox.herokuapp.com/ad_units/default");

            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log("Error while retrieving ad: " + uwr.error);
            }
            else
            {
                Debug.Log("Received ad unit");

                adUnitDataWrapper = AdUnitDataWrapper.CreateFromJSON(uwr.downloadHandler.text);
              
                switch(format)
                {
                    case "image":
                        {
                            StartCoroutine(GetImageTexture(adUnitDataWrapper.ad_unit_data.ad_unit_url));
                            break;
                        }

                    case "video":
                        {
                            StartCoroutine(GetMovieTexture("https://unity3d.com/files/docs/sample.ogg"));
                            break;
                        }
                }
            }
        }

        //called by getAdUnit
        IEnumerator GetImageTexture(string url)
         {
             UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

             yield return www.SendWebRequest();

             if (www.isNetworkError || www.isHttpError)
             {
                 Debug.Log("Error while getting ad texture:" + www.error);
             }
             else
             {
                 Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                 Debug.Log("Received ad texture");

                 ASRUAdUnit.GetComponent<Renderer>().material.mainTexture = myTexture;
              
                 ASRUAdUnit.SetActive(true);

                 var impression = new Impression(adUnitDataWrapper.ad_unit_data.id,
                     AdSpecterConfigPlugIn.appSessionWrapper.app_session.developer_app_id,
                     AdSpecterConfigPlugIn.appSessionWrapper.app_session.id
                 );

                 impressionWrapper = new ImpressionWrapper();
                 impressionWrapper.impression = impression;

                 var json = impressionWrapper.SaveToString();

                 StartCoroutine(PostImpression(json, "https://adspecter-sandbox.herokuapp.com/impressions"));

                 Debug.Log("Ad was seen");
                 startUpdate = true;
             }
         } 

        IEnumerator GetMovieTexture(string url)
        {
            UnityWebRequest www = UnityWebRequestMultimedia.GetMovieTexture(url);
            
            yield return www.SendWebRequest();
           
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("Error while getting ad texture:" + www.error);
            }
            else
            {
                MovieTexture myTexture = (DownloadHandlerMovieTexture.GetContent(www));
                Debug.Log("Received ad texture");

                ASRUAdUnit.GetComponent<Renderer>().material.mainTexture = myTexture;
                MovieTexture movie = ASRUAdUnit.GetComponent<Renderer>().material.mainTexture as MovieTexture;
               /* AudioSource audio = ASRUAdUnit.GetComponent<AudioSource>();
                audio.clip = movie.audioClip;
                Debug.Log(movie.audioClip);
                audio.Play();*/
                movie.Play();
                
            }
      
            //ASRUAdUnit.SetActive(true);

            var impression = new Impression(adUnitDataWrapper.ad_unit_data.id,
                AdSpecterConfigPlugIn.appSessionWrapper.app_session.developer_app_id,
                AdSpecterConfigPlugIn.appSessionWrapper.app_session.id
            );

            impressionWrapper = new ImpressionWrapper();
            impressionWrapper.impression = impression;

            var json = impressionWrapper.SaveToString();

            Debug.Log("line before post impression");
            StartCoroutine(PostImpression(json, "https://adspecter-sandbox.herokuapp.com/impressions"));

            Debug.Log("Ad was seen");
            startUpdate = true;
        }
        
        IEnumerator PostImpression(string json, string url)
        {
            var uwr = new UnityWebRequest(url, "PUT");

            if (json != "")
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            }

            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log("Error While Sending Impression: " + uwr.error);
            }
            else
            {
                Debug.Log("Received response");

                impressionWrapper = ImpressionWrapper.CreateFromJSON(uwr.downloadHandler.text);
            }
        }

        public void DetectClickThrough()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log("hit.transform.name" + hit.transform.name);

                    if (hit.transform.name == "ASRUAdUnit")
                    {
                        Debug.Log("Clicked");

                        Application.OpenURL(adUnitDataWrapper.ad_unit_data.click_url);
                        var json = impressionWrapper.SaveToString();
                        var impressionId = impressionWrapper.impression.id;

                        StartCoroutine(PostImpression("", string.Format("https://adspecter-sandbox.herokuapp.com/impressions/{0}/clicked", impressionId)));
                    }
                }
            }
        }
    }


    public class AdSpecterConfigPlugIn : MonoBehaviour
    {
        public static string appSessionId;
        public static AppSessionWrapper appSessionWrapper;

        public bool loadAds = false;

        public void AuthenticateUser(string developerKey)
        {
            var appSetup = new AppSetup(developerKey);
            var postData = appSetup.SaveToString();
            Debug.Log("appSetup: " + appSetup);
            Debug.Log("postData: " + postData);

            var url = "https://adspecter-sandbox.herokuapp.com/developer_app/authenticate";

            StartCoroutine(ASRUSetDeveloperKey(postData, url));
            Debug.Log("done authentication");
        }


        IEnumerator ASRUSetDeveloperKey(string json, string url)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            AdLoaderPlugIn adLoader = gameObject.AddComponent<AdLoaderPlugIn>();

            UnityWebRequest uwr = UnityWebRequest.Put(url, bodyRaw);
            uwr.method = "POST";
            uwr.SetRequestHeader("Content-Type", "application/json");
       
            yield return uwr.SendWebRequest();
            
            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log("Error while setting developer key: " + uwr.error);
            }
            else
            {
                Debug.Log("Developer key set successfully");

                appSessionWrapper = AppSessionWrapper.CreateFromJSON(uwr.downloadHandler.text);

                loadAds = true;
            }
        }
    }
}
