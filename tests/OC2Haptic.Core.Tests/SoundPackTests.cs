using System;
using System.Collections.Generic;
using System.IO;
using OC2Haptic.Core;

internal static class SoundPackTests
{
    internal static void Add(Dictionary<string,Action> tests)
    {
        tests.Add("sound pack PCM16 stereo decodes signed samples and unknown chunks",Decode);
        tests.Add("sound pack rejects truncated compressed oversized or invalid WAV",InvalidWave);
        tests.Add("sound pack catalog excludes retired sets and isolates malformed manifests",Catalog);
        tests.Add("sound pack supplied assets preserve full approved durations",ActualAssets);
        tests.Add("sound pack permits full4050ms music and enforces5000ms boundary",LongMusic);
    }
    private static void Decode()
    {
        using(var stream=Wave(48000,2,1,16,new short[]{-32768,32767,0,16384},true))
        {
            var pcm=PcmWaveReader.Read(stream);
            Check(pcm.SampleRate==48000 && pcm.Channels==2 && pcm.Samples.Length==4);
            Check(pcm.Samples[0]==-1 && Math.Abs(pcm.Samples[1]-32767f/32768)<.00001 && pcm.Samples[2]==0 && pcm.Samples[3]==.5f);
        }
    }
    private static void InvalidWave()
    {
        foreach(var stream in new[]{Wave(48000,2,3,16,new short[4]),Wave(48000,2,1,8,new short[4]),
            Wave(48000,3,1,16,new short[6]),Wave(48000,2,1,16,new short[48000*2*6]),Wave(48000,2,1,16,new short[3])})
            using(stream)Reject(stream);
        using(var truncated=Wave(48000,2,1,16,new short[4])){truncated.SetLength(truncated.Length-1);Reject(truncated);}
        using(var invalid=new MemoryStream(new byte[64]))Reject(invalid);
    }
    private static void Catalog()
    {
        string root=Path.Combine(Directory.GetCurrentDirectory(),".cache","pack-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string good=Path.Combine(root,"turbo_swing");Directory.CreateDirectory(good);
            string manifest="Name = 超燃卡通大乐队\nStar1 = one.wav\nStar2 = two.wav\nStar3 = three.wav\nStar4 = four.wav\n";
            File.WriteAllText(Path.Combine(good,"pack.ini"),manifest);
            foreach(string retired in new[]{"joyful-cartoon-jazz","CRISP-REWARD"})
            {string folder=Path.Combine(root,retired);Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"pack.ini"),manifest);}
            string bad=Path.Combine(root,"bad");Directory.CreateDirectory(bad);
            File.WriteAllText(Path.Combine(bad,"pack.ini"),"Name=bad\nStar1=../escape.wav\nStar2=2.wav\nStar3=3.wav\nStar4=4.wav\n");
            int warnings=0;var packs=RewardSoundPack.Discover(root,_=>warnings++);
            Check(packs.Length==1 && packs[0].Id=="turbo_swing" && warnings==1);
            Check(packs[0].Name=="超燃卡通大乐队" && packs[0].FileFor(4)==Path.Combine(good,"four.wav"));
            Check(RewardSoundPack.Discover(Path.Combine(root,"missing"),null).Length==0);
        }
        finally{Directory.Delete(root,true);}
    }
    private static void ActualAssets()
    {
        string root=Path.Combine(Directory.GetCurrentDirectory(),"assets","audio");
        var packs=RewardSoundPack.Discover(root,null);Check(packs.Length==3);
        var expected=new Dictionary<string,int[]>{{"turbo_swing",new[]{1050,1570,2300,3850}},
            {"champion_fanfare",new[]{1130,1700,2480,4050}},{"kitchen_rock",new[]{1010,1530,2350,3800}}};
        foreach(var pack in packs)for(int star=1;star<=4;star++)using(var file=File.OpenRead(pack.FileFor(star)))
        {
            var pcm=PcmWaveReader.Read(file);Check(pcm.SampleRate==48000 && pcm.Channels==2 && pcm.DurationMs==expected[pack.Id][star-1]);
            float peak=0;foreach(float sample in pcm.Samples){Check(!float.IsNaN(sample) && Math.Abs(sample)<1);peak=Math.Max(peak,Math.Abs(sample));}
            Check(peak>.1 && Math.Abs(pcm.Samples[pcm.Samples.Length-1])<.0001);
        }
    }
    private static void LongMusic()
    {
        foreach(int ms in new[]{4050,5000})using(var stream=Wave(48000,2,1,16,new short[48000*2*ms/1000]))
            Check(PcmWaveReader.Read(stream).DurationMs==ms);
        using(var stream=Wave(48000,2,1,16,new short[48000*2*5001/1000]))Reject(stream);
    }
    private static MemoryStream Wave(int rate,int channels,int format,int bits,short[] samples,bool extra=false)
    {
        var stream=new MemoryStream();var w=new BinaryWriter(stream);
        w.Write(0x46464952);w.Write(0);w.Write(0x45564157);
        if(extra){w.Write(0x4b4e554a);w.Write(3);w.Write(new byte[]{1,2,3,0});}
        w.Write(0x20746d66);w.Write(16);w.Write((short)format);w.Write((short)channels);w.Write(rate);w.Write(rate*channels*bits/8);
        w.Write((short)(channels*bits/8));w.Write((short)bits);w.Write(0x61746164);w.Write(samples.Length*2);
        foreach(short x in samples)w.Write(x);stream.Position=4;w.Write((int)stream.Length-8);stream.Position=0;return stream;
    }
    private static void Reject(Stream stream){try{PcmWaveReader.Read(stream);}catch(FormatException){return;}catch(EndOfStreamException){return;}throw new Exception("invalid WAV accepted");}
    private static void Check(bool b){if(!b)throw new Exception("sound pack behavior mismatch");}
}
