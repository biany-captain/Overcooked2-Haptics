using System;
using System.Globalization;
using System.Text;
namespace OC2Haptic.Core
{
    public static class PatternCodec
    {
        public static bool TryParse(string text,bool celebration,out HapticPattern result)
        {
            result=null;if(string.IsNullOrEmpty(text)||text.Length>4096)return false;
            string[] parts=text.Split(';');if(parts.Length>64)return false;
            var segments=new HapticSegment[parts.Length];
            try {for(int i=0;i<parts.Length;i++) {var values=parts[i].Split(',');float left,right;int ms;
                if(values.Length!=3 || !float.TryParse(values[0],NumberStyles.Float,CultureInfo.InvariantCulture,out left) ||
                    !float.TryParse(values[1],NumberStyles.Float,CultureInfo.InvariantCulture,out right) ||
                    !int.TryParse(values[2],NumberStyles.Integer,CultureInfo.InvariantCulture,out ms))return false;
                segments[i]=new HapticSegment(left,right,ms);}
                result=celebration?HapticPattern.Celebration(segments):new HapticPattern(segments);return true;
            }catch(ArgumentException){return false;}
        }
        public static string Format(HapticPattern pattern)
        {var b=new StringBuilder();for(int i=0;i<pattern.Count;i++){var s=pattern.SegmentAt(i);if(i>0)b.Append(';');
            b.Append(s.Left.ToString("0.###",CultureInfo.InvariantCulture)).Append(',').Append(s.Right.ToString("0.###",CultureInfo.InvariantCulture))
                .Append(',').Append(s.DurationMs.ToString(CultureInfo.InvariantCulture));}return b.ToString();}
    }
}
