#CLIP 8101 ELFE - UMZUGS-MAHNUNG DSCHUNGEL 2
+sq_activate Zwerge range 100 limit 50

sq_text file Urwald
sq_audio open Clip_8101

+sq_pen set VollbildKamera TriggerPos
+sq_pen set ElfPos TriggerPos
+sq_pen setz ElfPos 15

+sq_pen move ElfPos {1.0 -0.3 0};
+sq_pen set ElfBeamPos ElfPos
+sq_pen move ElfBeamPos {-12 -6 0}

sq_wait elf
do_elf movescreen {600 340 -16}
#Elfe Text: Na Prima! Merk schon - du...
elf unfollowview
do_elf text 8101a {kopf_schuetteln} Na
#Elfe Text: ...WILLST NICHT AUF MICH H÷REN! Zieh endlich mit deinen Wiggles um! Die Torw‰chterin verschlieﬂt das Tor!
elf unfollowview
do_elf text 8101b {meckern|reden_a} Du
+do_elf hide


