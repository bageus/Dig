#Brains begutachten falsche Erfindung
sq_text file Kristall
sq_audio open Kris_145
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmokristall
#-----------------------------------------
sq_actor find Zwerg 200 5 3

+sq_pen set Brains [ Getobjpos Info_Pos_Zwerg ]
+sq_pen set Haupt P_obj
+sq_pen set Wiggle Haupt
+sq_pen set Brains2 Haupt
+sq_pen set BrainsAssi Haupt
+sq_pen set Brainshilfe Haupt
+sq_pen set Kamera1 Haupt

+sq_pen set Back1 1
+sq_pen set Back2 2
+sq_pen set Back3 3
+sq_pen set Back4 4
+sq_pen set Back5 5
+sq_pen set Kamera2 Brains

+sq_pen move Wiggle { 1.5 0 0 }
+sq_pen setz Wiggle 13

+sq_pen setz Kamera1 5

+sq_pen move Brains2 { -3 0 0 }
+sq_pen setz Brains2 13

+sq_pen move BrainsAssi { 3 0 0 }
+sq_pen setz BrainsAssi 12.5

+sq_pen set BrainsProf Wiggle
+sq_pen move BrainsProf { -2 0 -1 }

+sq_pen move Brainshilfe { 8 0 0 }
+sq_pen setz Brainshilfe 13


+sq_pen form Brains2 RowHorMi 3
+sq_pen form Brains Circle 3
+sq_pen form Brainshilfe RowHorMi 5

sq_wait 0

sq_camera fix Haupt 0.9 -0.5 0
do_action beam Wiggle 0
do_action rotate Haupt 0
do_wait time 3
sq_color 1 {193 163 255}
sq_color 2 {163 232 255}

sq_camera selset inout
sq_camera move Kamera1 0.8 0.05 -0.25 0.3
do_wait time 3
do_action anim breathe 0
do_action anim tired 0
do_wait time 1

sq_camera fix Brains 1.0 -0.2 0.7

sq_wait 1
sq_actor actionlist { 2 3 4 5 } { { { anim scout } { anim standloopa } { anim talkacpoc } { anim talkacpob } } }
do_action anim standloopc { 2 3 4 5 }
do_action rotate Haupt 1
do_text 0145a 1 Auto 145a
do_action rotate Haupt { 2 4 }
do_text 0145b 1 PosAc 145b

sq_wait none
do_action walk Brainshilfe { 1 2 4 }
do_action rotate Haupt { 3 5 }
do_wait time 1
do_action walk Brainshilfe { 1 2 4 }
do_wait time 0.5
do_action walk Brainshilfe 3
do_wait time 0.8
do_action walk Brainshilfe 5
do_wait time 1

start_fade 3 0
do_wait time 3.5
sq_actor actionlist { 1 2 3 4 5 } {}
do_action beam Brains2 { 3 4 5 }
do_action beam BrainsProf 1
do_action beam BrainsAssi 2
do_wait time 1
sq_actor actionlist { 3 4 5 } { { { anim standloopc } { anim standloopb } { anim standloopa } { anim talkacpoc } { anim talkacpob } } loop }
do_action rotate Haupt {2 3 4 5 }
do_action rotate back 1
sq_camera fix Haupt 1.4 -0.3 0.165
start_fade 3 1
sq_wait 1
do_wait time 1
do_text 0145e 1 Auto 145e

sq_wait 2
sq_actor actionlist 1 { { anim kontrol } { anim scratchhead } { anim kontrol } { anim invent_b } }
do_action anim kontrol 1
do_text 0145c 2 PosAc 145c
sq_wait 0
do_action rotate 2 0
+sq_pen set BrainsProfHome Kamera1
+sq_pen move BrainsProfHome { 10 0 0 }
+sq_pen setz BrainsProfHome 13

+sq_pen set BrainsAssiHome Kamera1
+sq_pen move BrainsAssiHome { 12 0 0 }
+sq_pen setz BrainsAssiHome 13

do_text 0145d 0 { talkacngc } 145d
do_action rotate 1 0
do_action walktired Back2 2

sq_camera fix 1 1 -0.1 0.4

sq_wait 1
do_text 0145f 1 NegReac 145f
do_text 0145g 1 NegReac 145g
do_action rotate 0 1
sq_camera fix 1 0.8 -0.1 -0.4
do_text 0145h 1 NegReac 145h
do_text 0145i 1 { showright talkrenga } 145i
sq_wait none
do_action run Back1 1
do_wait time 1
do_action run Back3 3
do_action run Back4 4
do_action run Back5 5
sq_wait 0
sq_camera fix 0 0.8 -0.1 0.8
do_action rotate right 0
do_text 0145j 0 { talkacpoc talkacngc talkacpoc } 145j
do_wait time 0.5
do_text 0145k 0 { talkrenga } 145k
+cancel_fade
sq_camera selset inout
do_wait time 2
do_action wait 0.3 { 1 2 3 4 5 }
sq_camera move 0 1.0 -0.1 0.8
+do_action beam Back1 1
+do_action beam Back2 2
+do_action beam Back3 3
+do_action beam Back4 4
+do_action beam Back5 5
+do_wait time 3
+sq_camera get
+do_wait
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changetheme brains
#-----------------------------------------

