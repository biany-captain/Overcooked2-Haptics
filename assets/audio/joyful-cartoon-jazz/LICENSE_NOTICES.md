# 音频素材与许可记录 / License & provenance

## 原创内容

- 四首庆祝短曲均为本次独立编写的 MIDI 旋律、和声、节奏与结构。
- 4 星的小型卡通人声与掌声为 Python 算法合成，没有剪辑商业歌曲，也没有使用第三方现场欢呼录音。
- 脚本包括专门针对该 MOD 的音频混合、淡入淡出与均衡逻辑。

## 第三方渲染音色：TimGM6mb

- **Name:** TimGM6mb.sf2
- **Authors:** Tim Brechbill (2004); David Bolton (2010); as listed in Debian `timgm6mb-soundfont` copyright notice.
- **License:** GNU General Public License version 2 (GPL-2.0).
- **Local source used:** `/usr/share/sounds/sf2/TimGM6mb.sf2` (not included in this ZIP).
- **Distribution treatment:** The WAV and OGG files use rendered instrumental sample sounds from the GPL-2.0 bank. For a cautious redistributable approach, treat the **rendered final audio files as GPL-2.0-only assets**, keeping license and attribution attached. Original MIDI notes remain separately authored.
- **Notice:** `LICENSE_GPL-2.0.txt` is included. Consult the exact source bank's licensing if distributing a modified bank or using it in a differently licensed game/MOD; this note is not a legal opinion.
- **Source package:** https://packages.debian.org/search?keywords=timgm6mb-soundfont

## Not used

- No `Fancy Day` or other commercially published music audio used.
- No Freesound / Pixabay / Kenney audio files copied into this pack.

## Public distribution checklist

1. Retain these license notices and `LICENSE_GPL-2.0.txt` alongside the music files.
2. Do not claim the instrument-sample recordings are solely proprietary.
3. If the MOD requires CC0-only audio assets or a different overall distribution license, re-render the **original MIDI** using verified permissively licensed instrument sources before releasing.
