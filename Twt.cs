using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    }
}
