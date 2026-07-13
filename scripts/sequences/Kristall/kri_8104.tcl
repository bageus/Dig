#CLIP 8104 ELFE - UMZUGS-MAHNUNG LORELEI (KRISTALL 1)
+sq_activate Zwerge range 100 limit 50
sq_audio open Clip_8104
sq_text file Kristall

+sq_pen set VollbildKamera TriggerPos
+sq_pen set ElfPos TriggerPos
+sq_pen setz ElfPos 15

+sq_pen move ElfPos {1.0 -0.3 0};
+sq_pen set ElfBeamPos ElfPos
+sq_pen move ElfBeamPos {-12 -6 0}

sq_wait elf
do_elf movescreen {600 340 -16}
elf unfollowview
#Elfe Text: Bis hierher hast du es wirklich gut gemacht! Aber wenn du die Wiggles nicht evakuierst, werden sie alle sterben! Hier stürzt bald alles ein!
do_elf text 8104a {reden_b|meckern} Bis
+do_elf hide






