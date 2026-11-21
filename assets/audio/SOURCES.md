# Audio sources and redistribution records

Imported2026-11-21 from the user's `Overcooked2-Haptics-Star-Music` delivery. Files are copied without editing, gain baking, trimming or re-encoding. Their authorship/provenance statements are supplied by the pack, not independently established composition history.

| File in joyful-cartoon-jazz | Source WAV SHA256 | Duration |
|---|---|---:|
| star_1_playful_spark.wav | d9096d80851e7880e7b2b231bb7e4eaf7b2c265e2f099c49341bbee03fa994e8 |0.98s|
| star_2_bouncy_skip.wav | a64bc6ae976226aeb442bd015ee1217865c3a980cd3d25fb633d6657cd859615 |1.49s|
| star_3_brass_victory.wav |8a9bb63564fe146fc112fd2b077a6ab4f566c50b9f9ca0da416799384b043e6a |2.16s|
| star_4_grand_kitchen_party.wav |11fbaf581ec97c066b3adec7c15414024ac52de55c2d152e3f2cc34d73ac38bd |3.25s|

The supplied LICENSE_NOTICES.md states that the composition/MIDI and algorithmic crowd/claps were independently created for this project. It treats these rendered audio assets as **GPL-2.0-only**, because they use TimGM6mb instrumental samples. Retain that notice, `licenses/LICENSE_GPL-2.0.txt` and `licenses/TimGM6mb_Debian_copyright.txt` with redistribution. SoundFont authors: Tim Brechbill2004 and David Bolton2010. No `.sf2`, game soundtrack, Fancy Day, Pixabay or Freesound recording is included.

Source-bank package reference: [Debian timgm6mb-soundfont](https://packages.debian.org/source/sid/timgm6mb-soundfont); upstream commit is recorded in the supplied Debian copyright notice. This records the supplied license treatment; it does not relicense the plugin code or make a broader legal conclusion about rendered works.

`source/midi` and `source/build_music.py` preserve the editable notes and original offline render script. Its Python/FluidSynth/SoundFont/ffmpeg dependencies are production tools, **not MOD runtime dependencies**. The script's original README/config/analysis/task are retained in source; its original folder references describe the delivery layout. Running it generates audio_wav/audio_ogg relative to the script, not the installed pack. Do not run it in the game folder. The JSON rumble candidates are retained as source only: the user chose formal v1.4 timing instead.

Original `crisp-reward` tones remain generated in code by RewardTone and require no external recording. The dropdown selects one pack for all four stars; there is no unprovided “超燃” pack. See docs/audio-packs.md for the local pack format and verification requirements.
