﻿using BaconBackend.DataObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;
using BaconBackend.Managers;

namespace BaconBackend.Helpers
{
    /// <summary>
    /// The type of content that a link points toward.
    /// </summary>
    public enum RedditContentType
    {
        /// <summary>
        /// A subreddit.
        /// </summary>
        Subreddit,
        /// <summary>
        /// A post within a subreddit.
        /// </summary>
        Post,
        /// <summary>
        /// A comment on a reddit post.
        /// </summary>
        Comment,
        /// <summary>
        /// A user.
        /// </summary>
        User,
        /// <summary>
        /// A URL linking somewhere other than reddit.
        /// </summary>
        Website
    }

    /// <summary>
    /// Content that a link posted on reddit links to.
    /// </summary>
    public class RedditContentContainer
    {
        /// <summary>
        /// This content's content type.
        /// </summary>
        public RedditContentType Type;
        /// <summary>
        /// The webpage this content links to, if the type is a website.
        /// </summary>
        public string Website;
        /// <summary>
        /// The subreddit this content links to, if the type is a subreddit.
        /// </summary>
        public string Subreddit;
        /// <summary>
        /// The user this content links to, if the type is a user.
        /// </summary>
        public string User;
        /// <summary>
        /// The post this content links to, if the type is a reddit post
        /// </summary>
        public string Post;
        /// <summary>
        /// The comment this content links to, if the type is a reddit comment.
        /// </summary>
        public string Comment;
    }

    /// <summary>
    /// Error type that occurs when posting a new reddit post.
    /// </summary>
    public enum SubmitNewPostErrors
    {
        /// <summary>
        /// No error occurred.
        /// </summary>
        None,
        /// <summary>
        /// An unknown error occurred.
        /// </summary>
        Unknown,
        /// <summary>
        /// The request specified an option that doesn't exist.
        /// </summary>
        InvalidOption,
        /// <summary>
        /// A Captcha is required to submit the post, and it was submitted incorrectly.
        /// </summary>
        BadCaptcha,
        /// <summary>
        /// The URL linked to in the submitted post is invalid.
        /// </summary>
        BadUrl,
        /// <summary>
        /// The subreddit posted to does not exist.
        /// </summary>
        SubredditNoexist,
        /// <summary>
        /// The subreddit posted to does not allow the logged in user to make this post.
        /// </summary>
        SubredditNotallowed,
        /// <summary>
        /// No subreddit was specified when posting.
        /// </summary>
        SubredditRequired,
        /// <summary>
        /// The subreddit posted to does not allow the logged in user to make this self-text post.
        /// </summary>
        NoSelfs,
        /// <summary>
        /// The subreddit posted to does not allow the logged in user to make this link post.
        /// </summary>
        NoLinks,
        /// <summary>
        /// The post attempt timed out, and failed to complete.
        /// </summary>
        InTimeout,
        /// <summary>
        /// Reddit disallows the app to post this cpntent, as it has exceeded the reddit API rate limit.
        /// </summary>
        Ratelimit,
        /// <summary>
        /// The subreddit posted to does not allow links to be posted to the domain this post linked to.
        /// </summary>
        DomainBanned,
        /// <summary>
        /// The subreddit posted to does not allow links to be resubmitted, and this post has already been posted.
        /// </summary>
        AlreadySub
    }

    /// <summary>
    /// A response from reddit when a new post is submitted.
    /// </summary>
    public class SubmitNewPostResponse
    {
        /// <summary>
        /// Whether the post was successfully posted.
        /// </summary>
        public bool Success;
        /// <summary>
        /// A link to the post that was successfully created, or the empty string if no post was created.
        /// </summary>
        public string NewPostLink = string.Empty;
        /// <summary>
        /// The error type that occurred when posting, or NONE if no error occurred.
        /// </summary>
        public SubmitNewPostErrors RedditError = SubmitNewPostErrors.None;
    }

    /// <summary>
    /// Miscellaneous static helper methods.
    /// </summary>
    public class MiscellaneousHelper
    {
        /// <summary>
        /// Called when the user is trying to comment on something.
        /// </summary>
        /// <returns>Returns the json returned or a null string if failed.</returns>
        public static async Task<string> SendRedditComment(BaconManager baconMan, string redditIdCommentingOn, string comment, bool isEdit = false)
        {
            string returnString = null;
            try
            {
                // Build the data to send
                var postData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("thing_id", redditIdCommentingOn),
                    new KeyValuePair<string, string>("text", comment)
                };

                var apiString = isEdit ? "api/editusertext" : "api/comment";

                // Make the call
                returnString = await baconMan.NetworkMan.MakeRedditPostRequestAsString(apiString, postData);
            }
            catch (Exception e)
            {
                TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to send comment", e);
                baconMan.MessageMan.DebugDia("failed to send message", e);
            }
            return returnString;
        }      

        /// <summary>
        /// Gets a reddit user.
        /// </summary>
        /// <returns>Returns null if it fails or the user doesn't exist.</returns>
        public static async Task<User> GetRedditUser(BaconManager baconMan, string userName)
        {
            User foundUser = null;
            try
            {
                // Make the call
                var jsonResponse = await baconMan.NetworkMan.MakeRedditGetRequestAsString($"user/{userName}/about/.json");

                // Parse the new user
                foundUser = await ParseOutRedditDataElement<User>(baconMan, jsonResponse);
            }
            catch (Exception e)
            {
                TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to search for user", e);
                baconMan.MessageMan.DebugDia("failed to search for user", e);
            }
            return foundUser;
        }

        /// <summary>
        /// Attempts to delete a post.
        /// </summary>
        /// <param name="baconMan"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public static async Task<bool> DeletePost(BaconManager baconMan, string postId)
        {
            try
            {
                // Build the data to send
                var postData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("id", "t3_" + postId)
                };


                // Make the call
                var returnString = await baconMan.NetworkMan.MakeRedditPostRequestAsString("api/del", postData);

                if(returnString.Equals("{}"))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to delete post", e);
                baconMan.MessageMan.DebugDia("failed to delete post", e);
            }
            return false;
        }

        /// <summary>
        /// Saves, unsaves, hides, or unhides a reddit item.
        /// </summary>
        /// <returns>Returns null if it fails or the user doesn't exist.</returns>
        public static async Task<bool> SaveOrHideRedditItem(BaconManager baconMan, string redditId, bool? save, bool? hide)
        {
            if(!baconMan.UserMan.IsUserSignedIn)
            {
                baconMan.MessageMan.ShowSigninMessage(save.HasValue ? "save item" : "hide item");
                return false;
            }

            var wasSuccess = false;
            try
            {
                // Make the data
                var data = new List<KeyValuePair<string, string>> {new KeyValuePair<string, string>("id", redditId)};

                string url;
                if (save.HasValue)
                {
                    url = save.Value ? "/api/save" : "/api/unsave";
                }
                else if(hide.HasValue)
                {
                    url = hide.Value ? "/api/hide" : "/api/unhide";
                }
                else
                {
                    return false;
                }

                // Make the call
                var jsonResponse = await baconMan.NetworkMan.MakeRedditPostRequestAsString(url, data);

                if(jsonResponse.Contains("{}"))
                {
                    wasSuccess = true;
                }
                else
                {
                    TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to save or hide item, unknown response");
                    baconMan.MessageMan.DebugDia("failed to save or hide item, unknown response");
                }
            }
            catch (Exception e)
            {
                TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to save or hide item", e);
                baconMan.MessageMan.DebugDia("failed to save or hide item", e);
            }
            return wasSuccess;
        }

        /// <summary>
        /// Submits a new reddit post
        /// </summary>
        public static async Task<SubmitNewPostResponse> SubmitNewPost(BaconManager baconMan, string title, string urlOrText, string subredditDisplayName, bool isSelfText, bool sendRepliesToInbox)
        {
            if (!baconMan.UserMan.IsUserSignedIn)
            {
                baconMan.MessageMan.ShowSigninMessage("submit a new post");
                return new SubmitNewPostResponse { Success = false };
            }

            try
            {
                // Make the data
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("kind", isSelfText ? "self" : "link"),
                    new KeyValuePair<string, string>("sr", subredditDisplayName),
                    new KeyValuePair<string, string>("sendreplies", sendRepliesToInbox ? "true" : "false")
                };
                data.Add(isSelfText
                    ? new KeyValuePair<string, string>("text", urlOrText)
                    : new KeyValuePair<string, string>("url", urlOrText));
                data.Add(new KeyValuePair<string, string>("title", title));

                // Make the call
                var jsonResponse = await baconMan.NetworkMan.MakeRedditPostRequestAsString("/api/submit/", data);

                // Try to see if we can find the word redirect and if we can find the subreddit url
                var responseLower = jsonResponse.ToLower();
                if (responseLower.Contains("redirect") && responseLower.Contains($"://www.reddit.com/r/{subredditDisplayName}/comments/"))
                {
                    // Success, try to parse out the new post link
                    var startOfLink = responseLower.IndexOf($"://www.reddit.com/r/{subredditDisplayName}/comments/");
                    if(startOfLink == -1)
                    {
                        return new SubmitNewPostResponse { Success = false };
                    }

                    var endofLink = responseLower.IndexOf('"', startOfLink);
                    if (endofLink == -1)
                    {
                        return new SubmitNewPostResponse { Success = false };
                    }

                    // Try to get the link
                    var link = "https" + jsonResponse.Substring(startOfLink, endofLink - startOfLink);

                    // Return
                    return new SubmitNewPostResponse { Success = true, NewPostLink = link};
                }

                // We have a reddit error. Try to figure out what it is.
                for(var i = 0; i < Enum.GetNames(typeof(SubmitNewPostErrors)).Length; i++)
                {
                    var enumName = Enum.GetName(typeof(SubmitNewPostErrors), i).ToLower(); ;
                    if (!responseLower.Contains(enumName)) continue;
                    TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to submit post; error: "+ enumName);
                    baconMan.MessageMan.DebugDia("failed to submit post; error: "+ enumName);
                    return new SubmitNewPostResponse { Success = false, RedditError = (SubmitNewPostErrors)i};
                }

                TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to submit post; unknown reddit error: ");
                baconMan.MessageMan.DebugDia("failed to submit post; unknown reddit error");
                return new SubmitNewPostResponse { Success = false, RedditError = SubmitNewPostErrors.Unknown };
            }
            catch (Exception e)
            {
                TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to submit post", e);
                baconMan.MessageMan.DebugDia("failed to submit post", e);
                return new SubmitNewPostResponse { Success = false };
            }
        }

        /// <summary>
        /// Uploads a image file to imgur.
        /// </summary>
        /// <param name="baconMan"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string UploadImageToImgur(BaconManager baconMan, FileRandomAccessStream imageStream)
        {
            //try
            //{
            //    // Make the data
            //    List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
            //    data.Add(new KeyValuePair<string, string>("key", "1a507266cc9ac194b56e2700a67185e4"));
            //    data.Add(new KeyValuePair<string, string>("title", "1a507266cc9ac194b56e2700a67185e4"));

            //    // Read the image from the stream and base64 encode it.
            //    Stream str = imageStream.AsStream();
            //    byte[] imageData = new byte[str.Length];
            //    await str.ReadAsync(imageData, 0, (int)str.Length);
            //    data.Add(new KeyValuePair<string, string>("image", WebUtility.UrlEncode(Convert.ToBase64String(imageData))));
            //    string repsonse = await baconMan.NetworkMan.MakePostRequest("https://api.imgur.com/2/upload.json", data);
            //}
            //catch (Exception e)
            //{
            //    baconMan.TelemetryMan.ReportUnexpectedEvent("MisHelper", "failed to submit post", e);
            //    baconMan.MessageMan.DebugDia("failed to submit post", e);
            //    return new SubmitNewPostResponse() { Success = false };
            //}
            throw new NotImplementedException("This function isn't complete!");
        }


        /// <summary>
        /// Attempts to parse out a reddit object from a generic reddit response
        /// json containing a data object. We can't do a generic DataObject here because
        /// the function handles data blocks that are in various depths in the json.
        /// </summary>
        /// <param name="baconMan"></param>
        /// <param name="originalJson"></param>
        /// <returns></returns>
        public static async Task<T> ParseOutRedditDataElement<T>(BaconManager baconMan, string originalJson)
        {
            // TODO make this async. If I try to Task.Run(()=> the parse the task returns but the
            // await never resumes... idk why.
            try
            {
                // Try to parse out the data object
                var dataPos = originalJson.IndexOf("\"data\":", StringComparison.Ordinal);
                if (dataPos == -1)
                {
                    return default(T);
                }
                var dataStartPos = originalJson.IndexOf('{', dataPos + 7);
                if (dataStartPos == -1)
                {
                     return default(T);
                }
                // There can be nested { } in the data we want
                // To do this optimally, we will just look for the close manually.
                var dataEndPos = dataStartPos + 1;
                var depth = 0;
                var isInText = false;
                while (dataEndPos < originalJson.Length)
                {
                    // If we find a " make sure we ignore everything.
                    if(originalJson[dataEndPos] == '"')
                    {
                        if(isInText)
                        {
                            // If we are in a text block look if it is an escape.
                            // If it isn't an escape, end the text block.
                            if(originalJson[dataEndPos - 1] != '\\')
                            {
                                isInText = false;
                            }
                        }
                        else
                        {
                            // We entered text.
                            isInText = true;
                        }
                    }

                    // If not in a text block, look for {}
                    if(!isInText)
                    {
                        // If we find an open +1 to depth
                        if (originalJson[dataEndPos] == '{')
                        {
                            depth++;
                        }
                        // If we find and end..
                        else if (originalJson[dataEndPos] == '}')
                        {
                            // If we have no depth we are done.
                            if (depth == 0)
                            {
                                break;
                            }
                            // Otherwise take one off.

                            depth--;
                        }
                    }
                              
                    dataEndPos++;
                }

                // Make sure we didn't fail.
                if(depth != 0)
                {
                    return default(T);
                }
                
                // Move past the last }
                dataEndPos++;

                var dataBlock = originalJson.Substring(dataStartPos, (dataEndPos - dataStartPos));
                return JsonConvert.DeserializeObject<T>(dataBlock);
            }
            catch(Exception e)
            {
                TelemetryManager.ReportUnexpectedEvent("MisHelper", "failed to parse data element", e);
            }
            return default(T);
        }

        /// <summary>
        /// Attempts to find some reddit content in a link. A subreddit, post or comments.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static RedditContentContainer TryToFindRedditContentInLink(string url)
        {
            var urlLower = url.ToLower();
            RedditContentContainer container = null;

            var uri = new Uri(urlLower);

            if (uri.Host.Equals("v.redd.it"))
            {
                container = new RedditContentContainer
                {
                    Type = RedditContentType.Website,
                    Website = urlLower
                };
            }
            // Try to find /r/ or r/ links
            else if (urlLower.StartsWith("/r/") || urlLower.StartsWith("r/"))
            {
                // Get the display name
                var subStart = urlLower.IndexOf("r/", StringComparison.Ordinal);
                subStart += 2;

                // Try to find the next / after the subreddit if it exists
                var subEnd = urlLower.IndexOf("/", subStart, StringComparison.Ordinal);
                if(subEnd == -1)
                {
                    subEnd = urlLower.Length;
                }

                // Get the name.
                var displayName = urlLower.Substring(subStart, subEnd - subStart).Trim();

                // Make sure we don't have trailing arguments other than a /, if we do we should handle this as we content.
                var trimmedLowerUrl = urlLower.TrimEnd();
                if (trimmedLowerUrl.Length - subEnd > 1)
                {
                    // Make a web link for this
                    container = new RedditContentContainer
                    {
                        Type = RedditContentType.Website,
                        Website = $"https://reddit.com/{url}"
                    };
                }
                else
                {
                    // We are good, make the subreddit link for this.
                    container = new RedditContentContainer
                    {
                        Type = RedditContentType.Subreddit,
                        Subreddit = displayName
                    };
                }
            }
            // Try to find /u/ or u/ links
            if (urlLower.StartsWith("/u/") || urlLower.StartsWith("u/"))
            {
                // Get the display name
                var userStart = urlLower.IndexOf("u/", StringComparison.Ordinal);
                userStart += 2;

                // Try to find the next / after the subreddit if it exists
                var subEnd = urlLower.IndexOf("/", userStart, StringComparison.Ordinal);
                if (subEnd == -1)
                {
                    subEnd = urlLower.Length;
                }

                // Get the name.
                var displayName = urlLower.Substring(userStart, subEnd - userStart).Trim();

                // Make sure we don't have trailing arguments other than a /, if we do we should handle this as we content.
                var trimmedLowerUrl = urlLower.TrimEnd();
                if (trimmedLowerUrl.Length - subEnd > 1)
                {
                    // Make a web link for this
                    container = new RedditContentContainer
                    {
                        Type = RedditContentType.Website,
                        Website = $"https://reddit.com/{url}"
                    };
                }
                else
                {
                    // We are good, make the user link.
                    container = new RedditContentContainer
                    {
                        Type = RedditContentType.User,
                        User = displayName
                    };
                }
            }
            // Try to find any other reddit link
            else if(urlLower.Contains("reddit.com/"))
            {
                // Try to find the start of the subreddit
                var startSub = urlLower.IndexOf("r/", StringComparison.Ordinal);
                if (startSub == -1) return container;
                startSub += 2;

                // Try to find the end of the subreddit.
                var endSub = FindNextUrlBreak(urlLower, startSub);

                if (endSub <= startSub) return container;
                // We found a subreddit!
                container = new RedditContentContainer
                {
                    Subreddit = urlLower.Substring(startSub, endSub - startSub), Type = RedditContentType.Subreddit
                };

                // Special case! If the text after the subreddit is submit or wiki don't do anything special
                // If we return null we will just open the website.
                if(urlLower.IndexOf("/submit", StringComparison.Ordinal) == endSub || urlLower.IndexOf("/wiki", StringComparison.Ordinal) == endSub || urlLower.IndexOf("/w/") == endSub)
                {
                    container = null;
                    urlLower = "";
                }

                // See if we have a post
                var postStart = urlLower.IndexOf("comments/", StringComparison.Ordinal);
                if (postStart == -1) return container;
                postStart += 9;

                // Try to find the end
                var postEnd = FindNextUrlBreak(urlLower, postStart);

                if (postEnd <= postStart) return container;

                // We found a post! Build on top of the subreddit
                container.Post = urlLower.Substring(postStart, postEnd - postStart);
                container.Type = RedditContentType.Post;

                // Try to find a comment, for there to be a comment this should have a / after it.
                if (urlLower.Length <= postEnd || urlLower[postEnd] != '/') return container;
                postEnd++;
                // Now try to find the / after the post title
                var commentStart = urlLower.IndexOf('/', postEnd);
                if (commentStart == -1) return container;
                commentStart++;

                // Try to find the end of the comment
                var commentEnd = FindNextUrlBreak(urlLower, commentStart);

                if (commentEnd <= commentStart) return container;
                // We found a comment!
                container.Comment = urlLower.Substring(commentStart, commentEnd - commentStart);
                container.Type = RedditContentType.Comment;
            }

            return container;
        }

        private static int FindNextUrlBreak(string url, int startingPos)
        {
            var nextBreak = startingPos;
            while (url.Length > nextBreak && (char.IsLetterOrDigit(url[nextBreak]) || url[nextBreak] == '_'))
            {
                nextBreak++;
            }
            return nextBreak;
        }

        /// <summary>
        /// Gets the complementary color of the one given.
        /// </summary>
        /// <param name="source">The color to find the complement of.</param>
        /// <returns>The complement to the input color.</returns>
        public static Color GetComplementaryColor(Color source)
        {
            var inputColor = source;
            // If RGB values are close to each other by a diff less than 10%, then if RGB values are lighter side,
            // decrease the blue by 50% (eventually it will increase in conversion below), if RBB values are on darker
            // side, decrease yellow by about 50% (it will increase in conversion)
            var avgColorValue = (byte)((source.R + source.G + source.B) / 3);
            var diffR = Math.Abs(source.R - avgColorValue);
            var diffG = Math.Abs(source.G - avgColorValue);
            var diffB = Math.Abs(source.B - avgColorValue);
            if (diffR < 20 && diffG < 20 && diffB < 20) //The color is a shade of gray
            {
                if (avgColorValue < 123) //color is dark
                {
                    inputColor.B = 220;
                    inputColor.G = 230;
                    inputColor.R = 50;
                }
                else
                {
                    inputColor.R = 255;
                    inputColor.G = 255;
                    inputColor.B = 50;
                }
            }

            var rgb = new Rgb { R = inputColor.R, G = inputColor.G, B = inputColor.B };
            var hsb = ConvertToHsb(rgb);
            hsb.H = hsb.H < 180 ? hsb.H + 180 : hsb.H - 180;
            //hsb.B = isColorDark ? 240 : 50; //Added to create dark on light, and light on dark
            rgb = ConvertToRgb(hsb);
            return new Color { A = 255, R = (byte)rgb.R, G = (byte)rgb.G, B = (byte)rgb.B };
        }

        private static Rgb ConvertToRgb(Hsb hsb)
        {
            // By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
            var chroma = hsb.S * hsb.B;
            var hue2 = hsb.H / 60;
            var x = chroma * (1 - Math.Abs(hue2 % 2 - 1));
            var r1 = 0d;
            var g1 = 0d;
            var b1 = 0d;
            if (hue2 >= 0 && hue2 < 1)
            {
                r1 = chroma;
                g1 = x;
            }
            else if (hue2 >= 1 && hue2 < 2)
            {
                r1 = x;
                g1 = chroma;
            }
            else if (hue2 >= 2 && hue2 < 3)
            {
                g1 = chroma;
                b1 = x;
            }
            else if (hue2 >= 3 && hue2 < 4)
            {
                g1 = x;
                b1 = chroma;
            }
            else if (hue2 >= 4 && hue2 < 5)
            {
                r1 = x;
                b1 = chroma;
            }
            else if (hue2 >= 5 && hue2 <= 6)
            {
                r1 = chroma;
                b1 = x;
            }
            var m = hsb.B - chroma;
            return new Rgb
            {
                R = r1 + m,
                G = g1 + m,
                B = b1 + m
            };
        }

        private static Hsb ConvertToHsb(Rgb rgb)
        {
            // By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
            var r = rgb.R;
            var g = rgb.G;
            var b = rgb.B;

            var max = Max(r, g, b);
            var min = Min(r, g, b);
            var chroma = max - min;
            var hue2 = 0d;
            if (chroma != 0)
            {
                if (max == r)
                {
                    hue2 = (g - b) / chroma;
                }
                else if (max == g)
                {
                    hue2 = (b - r) / chroma + 2;
                }
                else
                {
                    hue2 = (r - g) / chroma + 4;
                }
            }
            var hue = hue2 * 60;
            if (hue < 0)
            {
                hue += 360;
            }
            var brightness = max;
            double saturation = 0;
            if (chroma != 0)
            {
                saturation = chroma / brightness;
            }
            return new Hsb
            {
                H = hue,
                S = saturation,
                B = brightness
            };
        }
        private static double Max(double d1, double d2, double d3)
        {
            return Math.Max(d1 > d2 ? d1 : d2, d3);
        }
        private static double Min(double d1, double d2, double d3)
        {
            return Math.Min(d1 < d2 ? d1 : d2, d3);
        }

        private struct Rgb
        {
            internal double R;
            internal double G;
            internal double B;
        }

        private struct Hsb
        {
            internal double H;
            internal double S;
            internal double B;
        }
    }
}
