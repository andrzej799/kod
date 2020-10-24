

using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Facebook_Post_Manager_V._0._0
{
  internal class FacebookGroups
  {
    public FacebookAccount CurrentAccount;
    public List<Friend> Friends = new List<Friend>();
    public WebChecker Web;
    public string DTSG;
    public string JAZOEST;
    private Config CFG = new Config();
    public string UserAgent = "";

    public FacebookGroups(Messages Generator, Config CFG)
    {
      this.CFG = CFG;
      this.Web = new WebChecker(CFG);
      this.Web.UpdateDevices();
      //this.Web.CheckAuthHidden();
      this.UserAgent = CFG.UserAgent;
    }

    public void Start()
    {
      this.DTSG = (string) null;
      this.JAZOEST = (string) null;
      this.Friends = new List<Friend>();
      this.CurrentAccount = this.Web.DownloadAcc();
      if (this.CurrentAccount.Error)
        return;
      this.CurrentAccount.SetContainer();
      this.CheckAccount();
    }

    public void CheckAccount()
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create("https://d.facebook.com/buddylist.php");
      httpWebRequest.UserAgent = this.UserAgent;
      httpWebRequest.CookieContainer = this.CurrentAccount._Cookies;
      try
      {
        HttpWebResponse response = (HttpWebResponse) httpWebRequest.GetResponse();
        if (new Regex("active_status.php").Match(new StreamReader(httpWebRequest.GetResponse().GetResponseStream()).ReadToEnd()).Success)
        {
          this.Web.SaveCorrectAccount(this.CurrentAccount);
          this.GetFriendsOnline();
        }
        else
          this.Start();
      }
      catch (Exception ex)
      {
        this.Start();
      }
    }

    public void GetFriendsOnline()
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create("https://d.facebook.com/buddylist.php");
      httpWebRequest.UserAgent = this.UserAgent;
      httpWebRequest.CookieContainer = this.CurrentAccount._Cookies;
      try
      {
        HttpWebResponse response = (HttpWebResponse) httpWebRequest.GetResponse();
        string end = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()).ReadToEnd();
        this.CurrentAccount._Cookies = httpWebRequest.CookieContainer;
        foreach (Match match in Regex.Matches(end, "<a href\\=\"/messages/read/\\?fbid\\=(?<id>[0-9a-zA-Z-_:]*)\\&amp\\;click_type\\=buddylist#fua\" class\\=\"bp\">(?<name>[A-Za-z ĄąĘęŃńŁłÓóĆćŹźŚś]*)</a>"))
        {
          foreach (Capture capture in match.Captures)
          {
            string id = match.Groups["id"].Value;
            if (this.Friends.FindIndex((Predicate<Friend>) (x => x.Id == id)) == -1)
            {
              string[] strArray = match.Groups["name"].Value.Split(' ');
              this.Friends.Add(new Friend()
              {
                Id = id,
                Name = match.Groups["name"].Value,
                FirstName = strArray[0]
              });
            }
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error : " + ex?.ToString());
      }
      this.DownloadFriendOffline();
    }

    public void DownloadFriendOffline()
    {
      List<Friend> list = new List<Friend>();
      int num = 0;
      bool flag = true;
      while (flag)
      {
        HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create("https://d.facebook.com/friends/center/friends/?ppk=" + num.ToString() + "&tid=u_0_0&bph=" + num.ToString() + "#friends_center_main");
        httpWebRequest.UserAgent = this.UserAgent;
        httpWebRequest.CookieContainer = this.CurrentAccount._Cookies;
        try
        {
          HttpWebResponse response = (HttpWebResponse) httpWebRequest.GetResponse();
          string end = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()).ReadToEnd();
          this.CurrentAccount._Cookies = httpWebRequest.CookieContainer;
          MatchCollection matchCollection = Regex.Matches(end, "href\\=\"/friends/hovercard/mbasic/\\?uid\\=(?<id>[0-9a-zA-Z-_:]*)\\&amp\\;redirectURI\\=[0-9A-Za-z.%_&;]*friending_list_id\\=1\">(?<name>[A-Za-z ĄąĘęŃńŁłÓóĆćŹźŚś]*)</a>");
          Regex.Matches(end, ":(?<id>[0-9]*)");
          flag = matchCollection.Count > 2;
          foreach (Match match in matchCollection)
          {
            foreach (Capture capture in match.Captures)
            {
              string id = match.Groups["id"].Value;
              if (this.Friends.FindIndex((Predicate<Friend>) (x => x.Id == id)) == -1)
              {
                string[] strArray = match.Groups["name"].Value.Split(' ');
                list.Add(new Friend()
                {
                  Id = id,
                  Name = match.Groups["name"].Value,
                  FirstName = strArray[0]
                });
              }
            }
          }
        }
        catch (Exception ex)
        {
        }
        if (num > 50)
          flag = false;
        ++num;
      }
      list.Shuffle<Friend>();
      this.Friends.AddRange((IEnumerable<Friend>) list);
      this.JoinGroup(this.CFG.GroupId);
    }

    public void DownloadGroupPage(string groupId, string groupName, List<Friend> FriendsListToAdd)
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create("https://d.facebook.com/groups/members/search/?group_id=" + groupId + "&refid=18");
      httpWebRequest.UserAgent = this.UserAgent;
      httpWebRequest.CookieContainer = this.CurrentAccount._Cookies;
      try
      {
        HttpWebResponse response = (HttpWebResponse) httpWebRequest.GetResponse();
        string end = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()).ReadToEnd();
        Match match1 = new Regex("name\\=\"fb_dtsg\" value\\=\"(?<fb_dtsg>[0-9a-zA-Z-_:]*)\"").Match(end);
        if (match1.Success)
        {
          this.DTSG = match1.Groups["fb_dtsg"].Value;
          Console.WriteLine("DSTG GROUPS : " + match1.Groups["fb_dtsg"].Value);
        }
        Match match2 = new Regex("name\\=\"jazoest\" value\\=\"(?<jazoest>[0-9]*)\"").Match(end);
        if (match2.Success)
        {
          this.JAZOEST = match2.Groups["jazoest"].Value;
          Console.WriteLine("JAZOEST GROUPS : " + match2.Groups["jazoest"].Value);
        }
        Match match3 = new Regex("action\\=\\\"\\/groups\\/members\\/search\\/\\?ext\\=(?<ext>[0-9]*)\\&amp\\;hash\\=(?<hash>[A-Za-z0-9_\\-:]*)\"").Match(end);
        if (!match3.Success)
          return;
        Console.WriteLine("Chuj Dupa Link JEst");
        Console.WriteLine("Ext :" + match3.Groups["ext"].Value);
        string str1 = match3.Groups["ext"].Value;
        string str2 = match3.Groups["hash"].Value;
        Console.WriteLine("Hash :" + match3.Groups["hash"].Value);
        this.AddFriends("https://d.facebook.com/groups/members/search/?ext= " + str1 + " &hash=" + str2, FriendsListToAdd, groupId, groupName);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }

    public void JoinGroup(string groupId)
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create("https://d.facebook.com/groups/" + groupId + "?_rdr");
      httpWebRequest.UserAgent = this.UserAgent;
      httpWebRequest.CookieContainer = this.CurrentAccount._Cookies;
      try
      {
        HttpWebResponse response = (HttpWebResponse) httpWebRequest.GetResponse();
        string end = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()).ReadToEnd();
        Match match1 = new Regex("name\\=\"fb_dtsg\" value\\=\"(?<fb_dtsg>[0-9a-zA-Z-_:]*)\"").Match(end);
        if (match1.Success)
        {
          this.DTSG = match1.Groups["fb_dtsg"].Value;
          Console.WriteLine("DSTG GROUPS : " + match1.Groups["fb_dtsg"].Value);
        }
        Match match2 = new Regex("name\\=\"jazoest\" value\\=\"(?<jazoest>[0-9]*)\"").Match(end);
        if (match2.Success)
        {
          this.JAZOEST = match2.Groups["jazoest"].Value;
          Console.WriteLine("JAZOEST GROUPS : " + match2.Groups["jazoest"].Value);
        }
        Match match3 = new Regex("\\&amp\\;gfid\\=(?<gfid>[A-Za-z0-9_\\-:]*)\\&amp").Match(end);
        if (!match3.Success)
          return;
        Console.WriteLine("GfId :" + match3.Groups["gfid"].Value);
        this.SendJoinGroup("https://d.facebook.com/a/group/join/?group_id=" + this.CFG.GroupId + "&gfid=" + match3.Groups["gfid"].Value + "&refid=18");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }

    public void SendJoinGroup(string uri)
    {
      try
      {
        HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create(uri);
        httpWebRequest.UserAgent = this.UserAgent;
        httpWebRequest.CookieContainer = this.CurrentAccount._Cookies;
        httpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*; q=0.8,application/signed-exchange;v=b3";
        httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        httpWebRequest.Headers.Add("Accept-Encoding", "gzip, deflate, br");
        httpWebRequest.Headers.Add("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7,fr;q=0.6");
        httpWebRequest.ContentType = "application/x-www-form-urlencoded";
        httpWebRequest.AllowAutoRedirect = false;
        httpWebRequest.Method = "POST";
        byte[] bytes = Encoding.ASCII.GetBytes("fb_dtsg=" + Uri.EscapeDataString(this.DTSG) + "&jazoest=" + Uri.EscapeDataString(this.JAZOEST));
        httpWebRequest.Method = "POST";
        httpWebRequest.ContentLength = (long) bytes.Length;
        using (Stream requestStream = httpWebRequest.GetRequestStream())
        {
          requestStream.Write(bytes, 0, bytes.Length);
          requestStream.Close();
        }
        using (HttpWebResponse response = (HttpWebResponse) httpWebRequest.GetResponse())
        {
          if (response.StatusCode >= HttpStatusCode.MultipleChoices)
          {
            if (response.StatusCode <= (HttpStatusCode) 399)
              Console.WriteLine(this.CurrentAccount.Id + "|Przekierowanie : " + response.Headers["Location"]);
          }
        }
        this.MakeGroup();
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error : " + ex.ToString());
      }
    }

    public void MakeGroup()
    {
      List<FriendsSplited> source = new List<FriendsSplited>();
      int num = 0;
      FriendsSplited friendsSplited1 = new FriendsSplited();
      foreach (Friend friend in this.Friends)
      {
        friendsSplited1.FacebookFriends.Add(friend);
        ++num;
        if (num >= 10)
        {
          source.Add(friendsSplited1);
          friendsSplited1 = new FriendsSplited();
          num = 0;
        }
      }
      Console.WriteLine("Liczba paczke: " + source.Count<FriendsSplited>().ToString());
      foreach (FriendsSplited friendsSplited2 in source)
      {
        FriendsSplited item = friendsSplited2;
        new Thread((ThreadStart) (() => this.DownloadGroupPage(this.CFG.GroupId, this.CFG.GroupName, item.FacebookFriends))).Start();
      }
    }

    public void AddFriends(
      string uri,
      List<Friend> FriendListToAdd,
      string groupId,
      string groupName)
    {
      try
      {
        HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create(uri);
        httpWebRequest.UserAgent = this.UserAgent;
        httpWebRequest.CookieContainer = this.CurrentAccount._Cookies;
        httpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*; q=0.8,application/signed-exchange;v=b3";
        httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        httpWebRequest.Headers.Add("Accept-Encoding", "gzip, deflate, br");
        httpWebRequest.Headers.Add("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7,fr;q=0.6");
        httpWebRequest.ContentType = "application/x-www-form-urlencoded";
        httpWebRequest.AllowAutoRedirect = false;
        httpWebRequest.Method = "POST";
        string s = "fb_dtsg=" + Uri.EscapeDataString(this.DTSG) + "&jazoest=" + Uri.EscapeDataString(this.JAZOEST) + "&group_name=" + Uri.EscapeDataString(groupName) + "&group_id=" + groupId + "&query_term=" + "&add=" + Uri.EscapeDataString("Zaproś wybranych");
        foreach (Friend friend in FriendListToAdd)
          s = s + "&addees[" + friend.Id + "]=" + friend.Id;
        byte[] bytes = Encoding.ASCII.GetBytes(s);
        httpWebRequest.Method = "POST";
        httpWebRequest.ContentLength = (long) bytes.Length;
        using (Stream requestStream = httpWebRequest.GetRequestStream())
        {
          requestStream.Write(bytes, 0, bytes.Length);
          requestStream.Close();
        }
        using (HttpWebResponse response = (HttpWebResponse) httpWebRequest.GetResponse())
        {
          if (response.StatusCode < HttpStatusCode.MultipleChoices || response.StatusCode > (HttpStatusCode) 399)
            return;
          Console.WriteLine(this.CurrentAccount.Id + "|Przekierowanie : " + response.Headers["Location"]);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error : " + ex.ToString());
      }
    }
  }
}
