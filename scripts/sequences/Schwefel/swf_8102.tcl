#CLIP 8102 - ELFE - UMZUGS-MAHNUNG BRÜCKE (METALL) 1
+sq_activate Zwerge range 100 limit 50
sq_audio open Clip_8102
sq_text file Schwefel


+sq_pen set VollbildKamera TriggerPos
+sq_pen set ElfPos TriggerPos
+sq_pen setz ElfPos 20

+sq_pen move ElfPos {1.0 -0.3 0};
+sq_pen set ElfBeamPos ElfPos
+sq_pen move ElfBeamPos {-12 -6 0}

sq_wait elf
do_elf movescreen {600 340 -16}
elf unfollowview
#Elfe Text:Die Brücke hält wirklich nicht lange! Bring deine Wiggles rüber!... Ich will dir doch nur helfen!
do_elf text 8102a {reden_b|anfeuern|reden_b} Die
+do_elf hide



