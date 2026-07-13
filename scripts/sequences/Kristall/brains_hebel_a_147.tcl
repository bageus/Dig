sq_text file Kristall
sq_audio open Kris_147
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmokristall
#-----------------------------------------
+sq_wait none
+sq_pen set Elfe [Getobjpos Info_Pos_Zwerg]
+sq_pen set Schalter Elfe
+sq_pen move Schalter { -13.2 0 7 }
+sq_pen set Schalter2 Elfe
+sq_pen move Schalter2 { -12 0 7.5 }
+sq_pen set Kamera Elfe
+sq_pen move Kamera { -12 0 7 }
+sq_pen move Elfe { -12 -1 12 }
+sq_pen set Elfestart Elfe
+sq_pen move Elfestart { -10 -5 2 }
+sq_actor find Zwerg 20 1 0
sq_camera move Kamera 0.8 -0.1 0.85
do_wait camera
tasklist_clear [Actor 1]
+sq_actor find Zwerg 20 5 3
sq_wait 1
do_action run Schalter 1
sq_wait none
sq_actor actionlist { 3 4 5 } { { { anim scout } { anim standloopa } { anim talkacpoc } { anim talkacpob } } }
do_action anim standloopc { 3 4 5 }
sq_actor actionlist 0 { { anim invent_a } { { anim standloopa } { anim standloopb } { anim invent_b } { anim invent_c } } loop }
do_action anim standloopc 0

+sq_pen set Back0 0
+sq_pen set Prof Back0
+sq_pen set Back2 2
+sq_pen move Prof { -4 0 1 }

sq_color 2 Brains1
sq_color 1 Wiggle1
sq_color 0 Brains2

sq_wait 1
do_action rotate front 1
do_action anim scout 1
do_action rotate left 1
sq_wait none
do_action anim invent_b 1
do_wait time 1
sq_wait 1
do_action anim dontknow 1
set_anim [Actor 1] benda 0 1
sq_wait none
do_text 0147a 2 NoAnim 147a
do_action flee Schalter2 2
do_wait time 0.7
do_action anim shock 1
do_wait time 1
sq_actor actionlist 2 {}
sq_wait 2
do_action rotate 2 1
do_action walk Prof 0

do_text 0147b 2 { talkacpoc talkacpob } 147b
sq_camera fix Kamera 1.1 -0.2 -0.7
sq_pen move Prof { -2 0 0 }
do_text 0147c 2 { talkacnga talkacngb talkacnga } 147c
sq_camera fix 0 0.8 -0.1 -0.9
sq_actor actionlist 0 {}
sq_wait 0
sq_pen move Kamera { -2 0 0 }
sq_actor actionlist 2 { { { anim talkacnta } { anim talkacntb } { anim talkacntc } { anim standloopc } { anim wipenose } } loop }
do_text 0147d 0 { invent_b talkacnga } 147d
sq_camera fix Kamera 1.1 -0.1 -0.9
+sq_pen move Elfe { 2 1 -1 }
do_elf beam Elfestart
do_action walk Prof 2
do_elf move Elfe
do_action rotate front 1
do_text 0147e 0 { invent_a } 147e
sq_wait none
sq_actor actionlist 0 { { { anim talkacnta } { anim talkacntb } { anim talkacntc } { anim standloopc } { anim wipenose } } loop }
do_action rotate Prof 0
sq_wait elf
do_action rotate Elfe 1
do_wait
elf unfollowview
sq_actor eyes 1 { c c c c o o o o 9 o o o o o o o 9 c c c c c o o o c c c c c 9 c }
do_action anim insane 1
do_elf text 0147f {auffordern} 147f
elf unfollowview
do_elf lookat 1
elf unfollowview
sq_wait none
do_elf text 0147g {beleidigen|meckern} 147g
do_wait time 2
sq_camera fix Kamera 1.4 -0.1 -0.1
sq_wait elf
do_wait
sq_wait 1
do_text 0147h 1 { talkrenga } 147h
sq_wait none
do_text 0147i 1 Auto 147i
do_wait time 1
do_action walk Back0 0
do_action walk Back2 2
sq_wait elf
elf unfollowview
do_elf anim schmollen
do_elf lookat
sq_wait none
do_elf move Elfestart
do_wait time 3
+do_elf hide
sq_wait 1
sq_camera fix 1 1 -0.1 0.2
do_action rotate 0.3 1
do_text 0147j 1 Auto 147j
+do_action beam Back0 0
+do_action beam Back2 2
+do_wait time 2
#sq_camera get
+do_wait
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changetheme brains
#-----------------------------------------

