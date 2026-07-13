sq_text file Kristall
start_fade 0.5 0
sq_wait none
do_wait time 0.6
sq_audio open Fenris_135a
adaptive_sound changethemenow s135a
sq_pen set Fenris [Getobjpos Fenris_003]
sq_pen set Cam02 Fenris
sq_pen move Cam02 { -1.5 -4.3 0 }
sq_pen setz Cam02 14
sq_pen set Cam01 Fenris
sq_pen move Cam01 { -1.8 -3.3 0 }
sq_pen set Cam03 Fenris
sq_pen move Cam03 { -3 -3.3 0 }
sq_pen set Cam04 Fenris
sq_pen move Cam04 { 0 -3.3 0 }
sq_pen set TrollPos Fenris
sq_pen move TrollPos { -6.5 -3 1 }
sq_pen set TrollCam Fenris
sq_pen move TrollCam { -6.5 -3.3 0 }
do_wait
sq_object summon Troll TrollPos
do_wait
sq_color 0 {255 118 57}
sq_actor find Troll
set_roty [Actor 1] 4.71
sq_actor actionlist 1 { { anim troll.stehen_handtuchwarten } loop }
do_action anim troll.stehen_handtuchwarten 1
sq_camera selset inout
sq_camera fix TrollCam 0.8 -0.1 -0.2
do_wait
start_fade 3 1
set_anim [Actor 0] bathstandloop 0 1
do_wait time 1.6
set_anim [Actor 0] bathwhistlea 0 1
do_wait time 3.4
set_anim [Actor 0] bathwash 0 1
do_wait time 0.3

#sq_actor actionlist 0 { {  anim bathwhistlea } { anim bathwash } { anim bathtalkstart } { anim bathtalknothing } }
#do_action anim bathstandloop 0
sq_camera fix Cam03 1.7 -0.2 0.3
sq_pen move TrollCam { 3 -0.5 0 }
do_wait time 3
set_anim [Actor 0] bathtalkstart 0 1
sq_camera move Cam01 1.5 -0.2 -0.3 0.4
do_wait time 1.4
set_anim [Actor 0] bathtalknothing 0 1
do_wait time 1.6
sq_camera fix Cam01 0.9 0 -0.3
sq_wait 0
do_wait
gametime factor 0.8
do_text 135aa 0 { bathtalkbloop bathtalkbloop } 135aa { 30 10 }
sq_camera fix TrollCam 0.9 0.2 0.9
sq_actor actionlist 1 { { anim troll.stehen_handtuchsalut } { anim troll.stehen_handtuchlauf } }
sq_wait none
gametime factor 0.9
do_text 135ab 0 { bathtalkastart bathtalkaloop bathtalkaloop } 135ab { 30 10 }
do_wait time 0.5
sound play equake7 0.8
sq_screenvibe equake7
sq_wait 0
do_wait
do_action anim bathtalkastop 0
gametime factor 1
do_action anim bathtalkend 0
sq_wait none
sq_actor actionlist 0 { { anim bathwigglestart } {anim bathwiggleloop} {anim bathwigglestop} loopstart {anim bathwhistlea } loop }
sq_camera selset inout
sq_camera move Cam03 0.9 0.2 0.9 0.2
do_action anim bathwhistlea 0
do_wait time 3.5
sq_camera fix Cam04 0.9 -0.2 -0.6 0.2
do_wait time 2
start_fade 5 0
do_wait time 5.5
#+sq_music feen TriggerPos
+cancel_fade
+sq_object delete all
+do_wait
+adaptive_sound changethemenow wigglesburg

