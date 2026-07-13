#CLIP 8100 - ELFE - UMZUGS-MAHNUNG DSCHUNGEL 1
+sq_activate Zwerge range 100 limit 50
sq_audio open Clip_8100
sq_text file Urwald

+sq_pen set VollbildKamera TriggerPos
+sq_pen set ElfPos TriggerPos
+sq_pen setz ElfPos 15

+sq_pen move ElfPos {1.0 -0.3 0}
+sq_pen set ElfBeamPos ElfPos
+sq_pen move ElfBeamPos {-13 0 0}
#Elfe Action:Die Elfe kommt angeflogen

sq_wait elf

do_elf movescreen {600 340 -16}

#Elfe Text: Hallo?! Beeil dich mal, deine Wiggles und dein Zeug durchs Tor zu bringen! Die Torwächterin verschliesst es bald!
elf unfollowview
do_elf text 8100 {auffordern|reden_b|reden_a} Hallo
#Elfe Action rausbewegen
+do_elf hide



