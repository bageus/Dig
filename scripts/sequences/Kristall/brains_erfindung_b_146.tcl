#Brains sind begeistert von Disco
sq_text file Kristall
sq_audio open Kris_145
sq_actor find Zwerg 400 5 3

+sq_pen set Brains [ Getobjpos Info_Pos_Zwerg ]
+sq_pen set Haupt P_obj
+sq_pen set Wiggle Haupt
+sq_pen set Brains2 Haupt
+sq_pen set BrainsAssi Haupt
+sq_pen set Brainshilfe Haupt
+sq_pen set Kamera1 Haupt

+sq_pen set dance1 Haupt
+sq_pen set dance2 Haupt
+sq_pen set dance3 Haupt
+sq_pen set dance4 Haupt
+sq_pen set dance5 Haupt


+sq_pen set Kamera2 Brains

+sq_pen move dance1 { 0 0 -3.5 }
+sq_pen move dance4 { -0.9 0 1.5 }
+sq_pen move dance5 { 0.9 0 1.5 }

+sq_pen move dance2 { -3.0 0 5 }

+sq_pen move Brains { -3 0 6 }
#+sq_pen move Back1 { 0 0 5 }
#+sq_pen move Back2 { -2.0 0 6 }
#+sq_pen move Back3 {0.5 0 3 }
#+sq_pen move Back4 {-0.7 0 2.6 }
#+sq_pen move Back5 {-1.2 0 1 }

do_action rotate 1 2
do_action rotate Brains 1
do_action rotate 4 3
do_action rotate 3 4
do_action rotate Brains 5

sq_color 1 {193 163 255}
sq_color 2 {163 232 255}


+sq_pen move Wiggle { 1.5 0 0 }
+sq_pen setz Wiggle 13

+sq_pen setz Kamera1 5

+sq_pen move Brains2 { -3 0 0 }
+sq_pen setz Brains2 13

+sq_pen move BrainsAssi { 3 0 0 }
+sq_pen setz BrainsAssi 12.5

+sq_pen set BrainsProf Wiggle
+sq_pen move BrainsProf { -2 0 -1 }

+sq_pen move Brainshilfe { 0 0 0 }
+sq_pen setz Brainshilfe 10


+sq_pen form Brains2 RowHorMi 3
+sq_pen form Brains Circle 3
+sq_pen form Brainshilfe RowHorMi 5

sq_wait 0

sq_camera fix Haupt 0.9 -0.5 0
do_action beam Wiggle 0
do_action rotate Haupt 0
do_wait time 3

sq_actor actionlist { 2 3 4 5 } { { { anim invent_b } { anim standloopa } { anim standloopb } { anim standloopc } } loop }
do_action anim standloopa { 2 3 4 5 }

sq_camera selset inout
sq_camera move Kamera1 0.8 0.05 -0.25 0.3
do_wait time 3
do_action anim stretch 0
adaptive_sound changethemenow brains_disco
adaptive_sound volfact 70
do_action anim discoa 0
+sq_pen move Brains { -4 0 0 }
do_action anim discoa 0

sq_wait 1
sq_camera fix Brains 1.0 -0.2 0.7
do_action anim shock 1
+sq_pen move Brains { 4 0 0 }
do_action rotate Haupt 1
do_text 0146a 1 PosAc 146a
do_text 0146b 1 PosAc 146b

sq_wait none
do_text 0146c 1 NoAnim 146c
do_action flee Brainshilfe 1

sq_actor actionlist { 2 3 4 5 } {}
do_wait time 0.5
sq_actor actionlist { 3 4 5 } { { { anim talkacpob } { anim talkacpoc } } { { flee Brainshilfe } { run Brainshilfe } } }

do_action rotate Haupt 3
do_wait time 0.3
do_action rotate Haupt 5
do_wait time 0.1
do_action rotate Haupt 4
do_wait time 2

sq_actor express 2 normal_dizzy
sq_wait 2
do_action rotate Haupt 2
do_action anim talkacngb 2

sq_wait none
do_action walktired Brainshilfe 2
do_wait time 3

start_fade 3 0
do_wait time 3
sq_actor actionlist { 1 2 3 4 5 } {}
do_action rotate front 1
do_action rotate front 2
do_action rotate front 3
do_action rotate back 4
do_action rotate back 5
+do_action beam dance1 1
+do_action beam dance2 2
+do_action beam dance3 3
+do_action beam dance4 4
+do_action beam dance5 5
do_action rotate back 4
do_action rotate back 5

sq_camera fix Haupt 1.0 -0.2 0
sq_actor actionlist 2 { { anim getcomfort } loop }
do_action anim getcomfort 2
sq_actor actionlist 1 { { { anim djc } { anim djc } { anim dja } { anim djc } } loop }
do_action anim dja 1
set_anim [obj_query this -class Disco -owner 3 -limit 1] disco.anim 0 2
sq_wait none
start_fade 3 1
do_wait time 1.5
+adaptive_sound volfact 100
do_action rotate front { 4 5 }
do_wait time 1.5

#Start Tanzszene ______________________

+sq_pen move dance3 { 0 0 2 }
+sq_pen move dance4 { 0 0 -2 }
+sq_pen move dance5 { 0 0 -2 }

sq_wait { 3 4 5 }
gametime factor 1.66
do_action anim showleft { 3 4 5 }
sq_camera fix Haupt 0.7 -0.1 0.8
do_action anim showright { 3 4 5 }
sq_camera move Haupt 0.7 -0.1 -0.8 0.15
do_action anim discoa { 3 4 5 }
do_action anim discoa { 3 4 5 }
do_action anim discoa { 3 4 5 }
do_action anim showleft { 3 4 5 }
do_action anim showright { 3 4 5 }
do_action anim discoa { 3 4 5 }
do_action anim discoa { 3 4 5 }
do_action anim discoa { 3 4 5 }
sq_camera fix Haupt 0.7 -0.3 -0.1
do_action anim showright { 3 4 5 }
sq_camera move Haupt 0.7 0.1 0.1 0.1
do_action anim talkrepoc { 3 4 5 }
sq_wait none
sq_actor actionlist 3 { { anim discoc } { anim discoc } { zombie dance3 } { anim discoa } loopstart { anim discoc } { anim discoc } { anim discoa } { anim discoa } loop }
sq_actor actionlist 4 { { anim discoc } { rotate back } { sneak dance4 } { rotate front } loopstart { anim discoc } { anim discoc } { anim discoa } { anim discoa } loop }
sq_actor actionlist 5 { { anim discoc } { rotate back } { sneak dance5 } { rotate front } loopstart { anim discoc } { anim discoc } { anim discoa } { anim discoa } loop }

do_action anim discoc { 3 4 5 }
do_wait time 10

#Ende Tanzszene _______________________

sq_wait none
sq_camera fix 2 0.8 -0.2 -0.5
do_wait time 3
sq_wait 0
sq_camera fix 0 0.9 -0.1 0.8
do_action rotate front 0
sq_actor eyes 0 { c 8 8 8 8 8 8 8 8 8 8 8 8 8 8 c }
sq_actor mouth 0 { c 10 10 10 10 10 11 11 11 11 10 10 10 10 10 c }
do_action anim talkpangb 0
sq_camera selset inout
+sq_pen move Brains { -8 0 0 }
do_action rotate Brains 0
sq_wait none
sq_camera move Brains 1.5 -0.3 0.4 0.1
+gametime factor 1
do_action walkfit Brains 0
do_wait camera
do_wait time 2
+sq_camera get
+sq_actor actionlist { 1 2 3 4 5 } {}
do_wait
+adaptive_sound changetheme brains
+adaptive_sound marker brains [get_pos this]


