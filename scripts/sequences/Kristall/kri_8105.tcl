#CLIP 8105 - ELFE - UMZUGS-MAHNUNG LORELEI (KRISTALL 2)
+sq_activate Zwerge range 100 limit 50
sq_audio open Clip_8105
sq_text file Kristall

+sq_pen set VollbildKamera TriggerPos
+sq_pen set ElfPos TriggerPos
+sq_pen setz ElfPos 15

+sq_pen move ElfPos {1.0 -0.3 0}
+sq_pen set ElfBeamPos ElfPos
+sq_pen move ElfBeamPos {-12 -6 0}

sq_wait elf
do_elf movescreen {600 340 -16}
elf unfollowview
#Elfe Text: Bring deine Wiggles SOFORT aus den Höhlen an der Lorelei vorbei! Es wird gleich alles einstürzen! BITTE!
do_elf text 8105a {auffordern|warnen} Bring
+do_elf hide

