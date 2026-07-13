# WIGGLES STOSSEN AUF GRANIT
sq_text file Tournament
sq_audio open Clip_2110
adaptive_sound markerdisable
adaptive_sound changetheme atmotourn
+sq_actor find Zwerg 10 2 0
+sq_pen set Start_Wiggles TriggerPos
+sq_pen set Kamera TriggerPos
+sq_pen set Elfe_Start TriggerPos
+sq_pen set Felsen TriggerPos

+sq_pen move Kamera {0 4 0}
+sq_pen move Elfe_Start {-4 -15 10}
+sq_pen move Felsen {0 4 10}
+sq_pen set ElfTalk Felsen
+sq_pen move ElfTalk {1 -0.5 0}

do_wait time 1

+sq_pen set Wiggle1Pos 0
+sq_pen move Wiggle1Pos {-0.75 1 1}
+sq_pen set Wiggle2Pos 0
+sq_pen move Wiggle2Pos {0.75 -3 1}

sq_wait elf
do_action walk Wiggle1Pos 0
do_action walk Wiggle2Pos 1
do_elf beam Elfe_Start
sq_camera fix Kamera 1.2 -0.1 0.1

do_elf move ElfTalk

#do_elf text "Das ist Granit, hier kommst Du nicht durch!"
do_elf text 2110a {reden_a} Das


#do_elf text "Du muﬂt irgendwo anders buddeln!"
do_elf text 2110b {auffordern} Buddel
sq_actor eyes 0 { c c c 9 c c c c c c c c 9 c c c c c c c c c 9 c c c c c c c c 9 c c c c c c 9 c c c c c c c 9 c c c c c c c 9 c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c 9 c c c c c c c c }
sq_actor actionlist 0 {{anim wait} {anim scratch} {anim scratchhead} {anim wait} {anim lookup} {anim wait} {anim standloopa} {anim standloopc} {anim standloopb} {anim wait}}
#do_elf text "Ich guck mal, wie weit die anderen sind..."
do_action rotate front 0
do_elf text 2110c {reden_a} Ich

do_elf anim gucken_links
do_elf anim gucken_rechts

+sq_pen set Elfe_Ende ElfTalk
+sq_pen move Elfe_Ende {10 -1 2}

do_elf move Elfe_Ende

do_wait time 5

+do_elf hide
+adaptive_sound markerenable
+adaptive_sound changetheme tournament

