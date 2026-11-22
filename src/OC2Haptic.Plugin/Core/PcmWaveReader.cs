using System;
using System.IO;

namespace OC2Haptic.Core
{
    public sealed class PcmWaveData
    {
        public readonly float[] Samples;
        public readonly int Channels,SampleRate,DurationMs;
        internal PcmWaveData(float[] samples,int channels,int rate)
        {Samples=samples;Channels=channels;SampleRate=rate;DurationMs=(int)Math.Ceiling(samples.Length*1000.0/channels/rate);}
    }

    // External WAV is the only decoding boundary; no Unity API or file I/O in score Hooks.
    public static class PcmWaveReader
    {
        public const int MaxDurationMs=11000;
        public static PcmWaveData Read(Stream stream)
        {
            var reader=new BinaryReader(stream);
            if(reader.ReadInt32()!=0x46464952)throw Invalid();
            long end=8L+reader.ReadUInt32();
            if(end>stream.Length || end<12 || reader.ReadInt32()!=0x45564157)throw Invalid();
            int channels=0,rate=0;long dataAt=0;uint dataSize=0;bool hasFormat=false;
            while(stream.Position+8<=end)
            {
                int id=reader.ReadInt32();uint size=reader.ReadUInt32();long next=stream.Position+size+(size&1);
                if(next>end)throw Invalid();
                if(id==0x20746d66)
                {
                    if(hasFormat || size<16 || reader.ReadUInt16()!=1)throw Invalid();
                    channels=reader.ReadUInt16();rate=reader.ReadInt32();int byteRate=reader.ReadInt32();
                    int align=reader.ReadUInt16(),bits=reader.ReadUInt16();
                    if((channels!=1 && channels!=2) || (rate!=44100 && rate!=48000) || bits!=16 ||
                        align!=channels*2 || byteRate!=rate*align)throw Invalid();
                    hasFormat=true;
                }
                else if(id==0x61746164)
                {
                    if(dataAt!=0)throw Invalid();dataAt=stream.Position;dataSize=size;
                }
                stream.Position=next;
            }
            if(!hasFormat || dataAt==0 || dataSize==0 || dataSize%(channels*2)!=0 ||
                dataSize>(long)rate*channels*2*MaxDurationMs/1000)throw Invalid();
            var samples=new float[dataSize/2];stream.Position=dataAt;
            for(int i=0;i<samples.Length;i++)samples[i]=reader.ReadInt16()/32768f;
            return new PcmWaveData(samples,channels,rate);
        }
        private static FormatException Invalid(){return new FormatException("Expected bounded PCM16 WAV, mono/stereo, 44.1/48kHz, up to11s.");}
    }
}
