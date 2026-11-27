# Overcooked2-Haptics · 超燃星级音乐扩展 v3.0

这是 **全新额外音乐包**，不是 v2 原音乐包的替换品，也不是已加载进游戏的 EXE/DLL。

## 新增三套，每套四首，共 12 首

- **超燃卡通大乐队 (turbo_swing, 208 BPM)**：明亮铜管、急促爵士鼓和萨克斯回应；最接近《Fancy Day》欢乐方向但更燃。
- **冠军加冕号角 (champion_fanfare, 188 BPM)**：大铜管胜利号角、鼓点与强烈收尾；4 星尤其适合夺冠氛围。
- **厨房摇滚派对 (kitchen_rock, 214 BPM)**：电吉他、合成主旋律、摇滚鼓组；更现代，适合另类风格。

各套 1 星轻快、2 星热闹、3 星明显胜利、4 星最高档激昂，不是同一首简单提速。所有音乐都是本次创作的原创 MIDI 乐句与编排，通过 TimGM6mb 音色采样库渲染而成。不是市场歌曲截取，也没有声称为真人乐队实录。

## 使用

1. 解压 ZIP；打开 `试听与风格选择.html` 试听三套风格及各星级。
2. 点击“整套使用”或各星级独立选择，导出 JSON。
3. 将本包、JSON 与 `docs/CODEX_INTEGRATION.md` 交给 Codex，让现有 MOD 加载。不能仅将 ZIP 放入游戏目录就直接生效。
4. 预览音量默认 65%，真实 MOD 应保留玩家独立音量。

- `presets/<style>/wav/*.wav`：48kHz / 16bit / stereo / PCM
- `presets/<style>/ogg/*.ogg`：OGG Vorbis，可供 Unity MOD 加载
- `presets/<style>/midi/*.mid`：原始编曲，供更换音色、改谱
- `previews/`：每风格 1～4 星连续试听
- `config/ultra_star_music_config.json`：游戏插件接入映射数据

## 欢呼和掌声：关于 v2 反馈

**v2 的合成拟声在听感上不够自然，因此 v3 音乐片段没有继续混入那段效果。** 请优先使用真正录制、授权明确的短人群录音，分别对 cheer / applause 混音，和第 4 星音乐保持可独立调音和开关。

两条已查到 CC0 的真实录音（未混入本包，需打开页面获取授权文件）：

- Freesound Nox_Sound：Small Crowd Cheer and Applause，CC0  https://freesound.org/people/Nox_Sound/sounds/752709/
- Freesound Nox_Sound：Small Crowd Applause，CC0  https://freesound.org/people/Nox_Sound/sounds/752710/
- Freesound Sadiquecat：Applause Edited 1，CC0  https://freesound.org/people/Sadiquecat/sounds/777707/

请 **不要把这三条音乐文件当成已经随包附送**。不从未知来源拼凑真人录音。推荐选择 0.4～0.8 秒真实声音，音乐尾部 duck 约 3dB，欢呼与掌声各比音乐低 10～16dB，最终耳朵试听后调整。

## 商业音乐的版权

免费开源不意味着能无授权分发已有歌曲。本包没有附带未经授权的商业音乐。若以后购买了特定歌曲/录音的同步、复制、改编及软件分发所需权利，MOD 可通过外部音效包加载，保留授权凭证；用户本地选曲与项目官方发布区分处理。

## SoundFont 与版权

本项目编写了原创乐句/MIDI，但渲染时使用 Debian 的 `TimGM6mb.sf2` SoundFont。其包内声明的许可证为 GPL-2.0，**这不等于 CC0 或完全无约束**。为了可追溯性，本包保留相应 GPL-2.0 文本、Debian 版权说明；发布到 GitHub 时保留。有关该音色的录音输出究竟适用哪些条款应进一步核实，不能把本包误说成没有任何版权限制的商用成品。若希望完全规避这类不确定性，应采用许可边界清楚的 CC0 乐器音色重新渲染 MIDI。

## 质量边界

所有文件进行 WAV、OGG、MIDI 与 CRC 校验，检查时长、声道、峰值和配置路径。但还没有真人试听盲测或游戏内实际体验验证，不能把技术检查当作完成音质验收。
