﻿/*
    Copyright (c) 2011 Alastair Paterson

    Permission is hereby granted, free of charge, to any person obtaining a copy of this
    software and associated documentation files (the "Software"), to deal in the Software
    without restriction, including without limitation the rights to use, copy, modify,
    merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be included in all copies
    or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace Parse
{
    public class ParseClient
    {
        public String ApplicationId { get; set; }
        public String ApplicationKey { get; set; }
        public Int32 ConnectionTimeout { get; set; }

        private String Endpoint = "https://api.parse.com/1/classes";
        
        /// <summary>
        /// Returns a new ParseClient to interact with the Parse REST API
        /// </summary>
        /// <param name="appId">Your application ID (found in the dashboard)</param>
        /// <param name="key">Your application key (found in the dashboard)</param>
        public ParseClient(String appId, String key, Int32 timeout = 100000)
        {
            if (String.IsNullOrEmpty(appId) || String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException();
            }

            ApplicationId = appId;
            ApplicationKey = key;
            ConnectionTimeout = timeout;
        }

        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="ClassName">The name of the ParseObject's class</param>
        /// <param name="PostObject">The object to be created on the server</param>
        /// <returns>A ParseObject with the ObjectId and date of creation</returns>
        public ParseObject CreateObject(String ClassName, Object PostObject)
        {
            return JsonConvert.DeserializeObject<ParseObject>(PostDataToParse(ClassName, PostObject));
        }

        /// <summary>
        /// Updates a pre-existing ParseObject
        /// </summary>
        /// <param name="ClassName">The name of the object's class</param>
        /// <param name="PostObject">The updated values of the object</param>
        /// <param name="ObjectId">The ObjectId of the object</param>
        public void UpdateObject(String ClassName, Object PostObject, String ObjectId)
        {
            PostDataToParse(ClassName, PostObject, ObjectId);
        }

        /// <summary>
        /// Get one object identified by its ID from Parse
        /// </summary>
        /// <param name="ClassName">The name of the object's class</param>
        /// <param name="ObjectId">The ObjectId of the object</param>
        /// <returns>A dictionary with the object's attributes</returns>
        public Dictionary<String, String> GetObject(String ClassName, String ObjectId)
        {
            if (String.IsNullOrEmpty(ClassName) || String.IsNullOrEmpty(ObjectId))
            {
                throw new ArgumentNullException();
            }

            String resultJSON = GetFromParse(Endpoint + "/" + ClassName + "/" + ObjectId);
            return JsonConvert.DeserializeObject<Dictionary<String, String>>(resultJSON);
        }

        /// <summary>
        /// Search for objects on Parse based on attributes. 
        /// </summary>
        /// <param name="ClassName">The name of the class being queried</param>
        /// <param name="Query">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="Order">The name of the attribute used to order the results. Prefacing with '-' will reverse the results.</param>
        /// <param name="Limit">The maximum number of results to be returned (Default unlimited)</param>
        /// <param name="Skip">The number of results to skip at the start (Default 0)</param>
        /// <returns>An array of Dictionaries containing the objects</returns>
        public ParseObjectList GetObjectsWithQuery(String ClassName, Object Query, String Order = null, String Limit = null, String Skip = null)
        {
            if (String.IsNullOrEmpty(ClassName) || Query == null)
            {
                throw new ArgumentNullException();
            }
            return JsonConvert.DeserializeObject<ParseObjectList>(this.GetFromParse(Endpoint + "/" + ClassName, Query));
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="ClassName">The name of the class of the object to be deleted</param>
        /// <param name="ObjectId">The objectId of the object to be deleted</param>
        public void DeleteObject(String ClassName, String ObjectId)
        {
            if (String.IsNullOrEmpty(ClassName) || String.IsNullOrEmpty(ObjectId))
            {
                throw new ArgumentNullException();
            }

            WebClient webClient = new WebClient();
            WebRequest webRequest = WebRequest.Create(Endpoint + "/" + ClassName + "/" + ObjectId);
            webRequest.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Method = "DELETE";

            webRequest.Timeout = ConnectionTimeout;

            HttpWebResponse responseObject = (HttpWebResponse)webRequest.GetResponse();
            responseObject.Close();

            return;
        }

        //Private Methods
        private String GetFromParse(String endpointUrl, Object queryObject = null, String Order = null, Int32 Limit = 0, Int32 Skip = 0)
        {
            WebClient webClient = new WebClient();

            String finalEndpointUrl = endpointUrl + "?";

            if (queryObject != null)
            {
                finalEndpointUrl += "&where=" + System.Web.HttpUtility.UrlEncodeUnicode(JsonConvert.SerializeObject(queryObject));
            }

            if (Order != null)
            {
                finalEndpointUrl += "&order=" + System.Web.HttpUtility.UrlEncodeUnicode(Order);
            }

            if (Limit != 0)
            {
                finalEndpointUrl += "&limit=" + Limit.ToString();
            }

            if (Skip != 0)
            {
                finalEndpointUrl += "&skip=" + Skip.ToString();
            }

            WebRequest webRequest = WebRequest.Create(finalEndpointUrl);

            NetworkCredential streetCred = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Credentials = streetCred;
            webRequest.Method = "GET";
            webRequest.Timeout = ConnectionTimeout;

            HttpWebResponse b = (HttpWebResponse)webRequest.GetResponse();
           
            //TODO: error handling

            System.IO.Stream readerStream = b.GetResponseStream();
            System.IO.StreamReader uberReader = new System.IO.StreamReader(readerStream);

            String response = uberReader.ReadToEnd();

            b.Close();

            return response;
        }

        private String PostDataToParse(String ClassName, Object PostObject, String ObjectId = null)
        {
            if (String.IsNullOrEmpty(ClassName) || PostObject == null)
            {
                throw new ArgumentNullException();
            }
            
            if (String.IsNullOrEmpty(ObjectId) == false)
            {
                ClassName += "/" + ObjectId;
            }

            WebClient webClient = new WebClient();
            WebRequest webRequest = WebRequest.Create(Endpoint + "/" + ClassName);
            webRequest.Credentials = new NetworkCredential(ApplicationId, ApplicationKey);
            webRequest.Method = "POST";

            if(String.IsNullOrEmpty(ObjectId) == false)
            {
                webRequest.Method = "PUT";
            }

            String postString = JsonConvert.SerializeObject(PostObject);

            byte[] postDataArray = Encoding.UTF8.GetBytes(postString);

            webRequest.ContentLength = postDataArray.Length;
            webRequest.Timeout = ConnectionTimeout;
            webRequest.ContentType = "application/json";

            System.IO.Stream writeStream = webRequest.GetRequestStream();
            writeStream.Write(postDataArray, 0, postDataArray.Length);
            writeStream.Close();

            HttpWebResponse responseObject = (HttpWebResponse)webRequest.GetResponse();
            if (responseObject.StatusCode == HttpStatusCode.Created || true)
            {
                System.IO.StreamReader responseReader = new System.IO.StreamReader(responseObject.GetResponseStream());
                String responseString = responseReader.ReadToEnd();
                responseObject.Close();

                return responseString;
            }
            else
            {
                responseObject.Close();
                throw new Exception("New object was not created. Server returned code " + responseObject.StatusCode);
            }
        }
    }
}
