using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyZipper
{
    public class Twt
    {
        static public DateTime GetDateTime(long id)
        {
            // Twitterエポック(2010-11-04 01:42:54.657)
            long twitterEpoch = 1288834974657L;

            // UNIXエポック(1970/01/01 00:00:00.000)
            long unixEpoch = 62135596800000L;

            // タイムスタンプのビット数
            int timestampBits = 41;
            // タイムスタンプのシフト数
            int timestampShift = 22;
            // タイムスタンプのマスク
            long timestampMask = -1L ^ (-1L << timestampBits);

            // 0001/01/01 00:00:00.000 からの経過ミリ秒
            var timestamp =
                ((id >> timestampShift) & timestampMask)
                    + twitterEpoch
                    + unixEpoch;
            return new DateTime(
                timestamp * TimeSpan.TicksPerMillisecond,
                DateTimeKind.Utc
            );
        }

        static public string GetTwtID(string path)
        {
            var twtid = Path.GetFileName(path);
            return twtid;
        }

        static public string GetUploadDate(string path)
        {
            {
                var r = new Regex(@"(\d+) \d+ \d+-\d+-\d+");
                var m = r.Match(path);
                if (m.Success)
                {
                    var d = GetUploadDateTime(m.Groups[1].Value);
                    return d.ToString();
                }
            }

            //https://twitter.com/TwitterDev/status/17150x26x04x9820679
            {
                var r = new Regex(@"\d{8}\s+(\d+)");
                var m = r.Match(path);
                if (m.Success)
                {
                    var twt_id = m.Groups[1].Value;
                    Log.D($"{twt_id}");
                    var d = GetUploadDateTime(twt_id);
                    return d.ToString();
                }
             }

            return "";
        }

        static public DateTime GetUploadDateTime(string tweet_id_str)
        {
            var tweet_id = long.Parse(tweet_id_str);
            var d = Twt.GetDateTime(tweet_id);
            return d;
        }
    }
}
