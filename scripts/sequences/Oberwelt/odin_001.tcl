start_fade 0.5 0
adaptive_sound markerdisable
sq_text file Urwald
sq_audio open Obw_Odin001
sq_pen set Hand_Klingeln [Getobjpos Info_Pos_Zwerg]
sq_pen set Hand_Stupsen [Getobjpos Info_Pos_ZwergTmp]
sq_pen set Hand_Uhrnehmen [Getobjpos Info_Pos_Troll]
sq_pen set Hand_Gesten [Getobjpos Info_Pos_Spinne]
sq_pen set Start { 38.5 18.5 11 }
set_roty [Actor 0] 0
do_action beam Start 0

sq_pen set Cam01 0
sq_pen move Cam01 { 0.5 0 0 }
sq_pen set Text01 0
sq_pen move Text01 { 0.2 -0.24 1 }
sq_pen set Text02 0
sq_pen move Text02 { 0 -1.2 5 }
sq_pen set Text03 0
sq_pen move Text03 { 0.4 -0.51 0 }
sq_pen set Text04 0
sq_pen move Text04 { 0.8 -0.7 1 }
sq_pen set Text05 0
sq_pen move Text05 { 1.5 -1.3 3 }
sq_pen set Text06 0
sq_pen move Text06 { 1.5 -1.1 3 }
sq_pen set Text07 0
sq_pen move Text07 { 0.5 -0.7 3 }
sq_pen set wech 0
sq_pen move wech { 20 0 0 }
sq_pen set Cam02 0
sq_pen move Cam02 { -5 0.3 0 }
sq_pen set Cam03 0
sq_pen move Cam03 { 1 -0.2 0 }
sq_pen set Cam04 0
sq_pen move Cam04 { 0.25 -0.2 0 }
sq_pen set Cam05 0
sq_pen move Cam05 { 0 -0.2 0 }
sq_pen set Cam06 0
sq_pen move Cam06 { 0 -0.4 0 }
sq_pen set Cam07 0
sq_pen move Cam07 { 0.4 -0.3 0 }
sq_pen set Cam08 0
sq_pen move Cam08 { 0.4 -0.4 1.5 }
sq_pen set Cam09 0
sq_pen move Cam09 { -0.5 -0.2 1.0 }
sq_pen set CamEnd 0
sq_pen move CamEnd { -0.5 -0.5 1.0 }
sq_actor express 0 good_sleep
sq_actor actionlist 0 { { anim kingbedloopb } loop }
do_action anim kingbedloopb 0
sq_object summon Hand_Gottes Hand_Uhrnehmen
sq_object summon Zwerg
do_wait
call_method [Object 1] init
sq_object summon Fifi wech
sq_object summon Zwerg
do_wait
call_method [Object 3] init
sq_object summon Dummy_Koenigsschlafmuetze
do_wait

set_visibility [Object 1] 0
set_autolight [Object 1] 0
set_visibility [Object 3] 0
set_autolight [Object 3] 0
link_obj [Object 4] [Actor 0] 4
sq_actor find Hand_Gottes
sq_actor find Zwerg

set_roty [Actor 1] 1.35336
sq_camera fix Cam02 0.9 -0.1 0.5
do_action beam Text01 2
do_wait time 4
start_fade 6 1
sq_camera addset inout2 0.0 { { s 0.7 1.0 } { s 1.0 0.0 } }


sq_camera selset inout
sq_camera move Cam01 0.8 -0.1 0.7 0.05
do_wait time 3
adaptive_sound changetheme koenigauftrag
do_wait time 17
sq_camera move Cam03 0.7 -0.1 0.7 0.3
do_wait time 4
sq_wait 1
do_action anim takeclocka 1
sq_actor actionlist 0 { { anim kingwithoutclock } loop }
set_anim [Actor 1] takeclockb 0 1
do_action anim kingwithoutclock 0
do_wait time 3
sq_camera move Cam04 0.8 -0.1 0.7 0.2
do_action beam Hand_Klingeln 1
set_roty [Actor 1] 1.23483
set_rotx [Actor 1] 6.2827
do_wait time 3
sq_wait none
do_action anim startclock 1
do_wait time 1.3
do_wait time 0.4
sq_actor actionlist 0 {}
sq_wait none
set_anim [Actor 0] kingwakeupstart 0 1
-sound play wecker_l 0.5
do_action anim clockring 1
sq_actor eyes 0 { c 2 2 2 2 2 2 2 2 2 2 2 2 8 8 8 }
sq_actor mouth 0 { c 2 2 2 2 2 2 2 2 2 2 2 2 3 3 3 }
do_wait time 0.1
sq_wait 0
do_action anim kingwakeuploop 0
sq_actor express 0 bad_dizzy
sq_camera fix Cam05 0.7 -0.1 0.3 0.5
sq_color 2 Odin
sq_color 0 Wiggle1
do_text 001a 2 Auto 001a Auto Off
do_action anim kingwakeupstop 0
do_action beam wech 1
sq_camera move Cam05 0.7 -0.3 0.3 0.6
sq_actor eyes 0 { c c c c o o o o o 3 3 3 3 3 3 3 3 3 3 }
sq_actor mouth 0 { c c c c c c c c c c 0 15 14 14 14 14 14 14 0 c }
sq_wait none
do_action anim kinglookupa 0
do_wait time 1.4
sq_actor actionlist 0 { { anim kinglookupbloop } loop }
sq_actor eyes 0 { c 7 }

do_text 001b 0 {kinglookupbloop kinglookupbloop kinglookupbloop kinglookupbloop} 001b
do_wait time 3.5
do_action anim kinglookupbloop 0
do_wait time 0.5
sq_camera move Cam07 0.9 -0.3 0.6 0.1
do_text 001c 2 Auto 001c Auto Off
do_wait time 2
do_wait time 5
do_text 001d 2 Auto 001d Auto Off
do_wait time 2
set_rotx [Actor 1] 0
set_roty [Actor 1] 1.433
sq_camera fix Cam06 0.7 -0.2 0.5 4
do_action beam Hand_Stupsen 1
do_action anim tabnose 1
do_wait time 0.5
sq_actor eyes 0 { 7 }
do_text 001e 2 Auto 001e Auto Off
set_roty [Actor 2] 0
do_wait time 0.5
sq_wait 0
sq_actor actionlist 0 {}
do_action anim kinglookupbstop 0
sq_actor express 0 normal_tired
sq_actor eyes 0 { c c c c c c u u u 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 u 7 }
sq_actor mouth 0 { c c c c c c c c 15 14 9 9 9 9 9 9 9 9 9 9 11 10 c }
sq_sound 001ea 0
do_action anim kingtired 0
do_action beam Hand_Gesten 1
set_visibility [Actor 1] 0
sq_camera fix Cam06 0.8 -0.2 0.5 4
sq_actor express 0 normal_tired
do_text 001f 0 { kingtalka kingtalkb } 001f
do_wait time 0.5
sq_camera fix Cam07 0.65 -0.2 0.8 4
do_wait time 0.5
sq_actor eyes 0 { o o o o o o 4 4 4 4 4 4 4 4 4 4 4 c 7 }
do_text 001g 0 { kingtalka kingtalkb } 001g
set_visibility [Actor 1] 1
sq_actor actionlist 0 { { anim kingsitstandanim } loop }
do_wait time 1
sq_actor express 0 normal_tired
do_text 001h 0 { kingtalka kingtalkb kingtalka } 001h
sq_actor actionlist 1 { { anim gestureb } { anim gesturebloop } { anim gesturebloop } { anim gesturebloop } { anim gesturec } { anim gesturecloop } { anim gesturecloop } { anim gesturecloop } { anim gestured } { anim gesturee } { anim gesturee }}
do_action anim gesturea 1
sq_camera fix Cam06 0.9 -0.1 0.9 4
sq_wait 2
do_action beam Text05 2
do_text 001i 2 Auto 001i Auto Off
do_wait time 0.4
do_text 001j 2 Auto 001j Auto Off
do_wait time 1
sq_camera move Cam08 0.8 -0.1 0.8 0.1
sq_object move 1 Text06 0.1
do_text 001k 2 NoAnim 001k { 50 10 } Off
do_text 001l 2 NoAnim 001l { 50 10 } Off
do_wait time 0.5
do_text 001m 2 NoAnim 001m { 50 10 } Off
do_wait time 0.4
start_fade 0.5 0
do_wait time 0.5
sq_wait none
#------FENRIS------------------------------------------------------------------

sq_actor find Fenris_001 200
set_visibility [Actor 3] 0
sq_actor find Fifi 200
sq_actor find Zwerg
sq_pen set zurueck 3
sq_pen set fump 3
sq_pen move fump { 0 -1.5 5 }
sq_pen set fump2 3
sq_pen move fump2 { 0 -3 0 }
sq_pen set wech 3
sq_pen move wech { -50 -1.5 0 }
sq_pen set CamF1 3
sq_pen move CamF1 { 0 -1.5 0 }
sq_pen set CamF2 3
sq_pen move CamF2 { 0 -3 0 }
sq_pen set FPos2 3
sq_pen move FPos2 { 0 0 0 }
sq_pen set FPos3 3
sq_pen move FPos3 { 0 0 4 }
sq_pen set FPos4 3
sq_pen move FPos4 { 0 0 -4 }
sq_pen set FText11 3
sq_pen move FText11 { 2 -3.6 3 }
sq_pen set FText12 3
sq_pen move FText12 { 9 -8 3 }
sq_pen set FText21 3
sq_pen move FText21 { -2 1 8 }
do_action beam FPos2 3
set_roty [Actor 3] 0
do_action beam FPos4 4
fow_wech [parse_pos FPos3]
sq_camera fix CamF1 1.2 0.05 -0.065
do_wait time 0.5
start_fade 4 1
do_wait time 1
sq_color 5 Wiggle1
#sq_actor actionlist 4 { { anim standanim } loop }
do_action beam FText11 2
do_action beam FText21 5
sq_wait 2
sq_actor actionlist 4 { { anim bark } { anim bark } { walk FPos3 } { anim jump } { anim bark } { anim fifi.bein_heben} { anim jump } { anim jump } { anim fifi.bein_heben} }
do_action anim jump 4

do_text 001n 2 NoAnim 001n { 30 10 } Off
sq_wait 5
do_text 001o 5 NoAnim 001o { 10 80 } Off
do_wait time 1.5
sq_object summon Dummy_L1 fump
set_anim [Object 5] l1.null 0 2;set_visibility [Object 5] 0
do_text 001p 5 NoAnim 001p { 10 80 } Off
do_wait time 0.5
sq_wait 2
do_text 001q 2 NoAnim 001q { 50 10 } Off
sq_actor actionlist 4 {}
sq_wait none
sq_object move 1 FText12 0.1
sq_camera move CamF1 1.9 0.1 -0.065 0.2
set_visibility [Object 5] 1; set_anim [Object 5] l1.standard 0 1
do_action anim oldfifi 4
do_wait time 0.5
-sound play equake7 1
sq_screenvibe equake7
do_text 001r 2 NoAnim 001r { 50 10 } Off
sq_object move 1 FText12 0.1
do_wait time 1.0
sq_object move 5 fump2 0.05
change_particlesource [Object 5] 0 33 { 0 0 0 } { 0 -0.1 0 } 100 10 0
set_particlesource [Object 5] 0 1
do_wait time 1.6
set_visibility [Actor 4] 0
set_visibility [Actor 3] 1
sq_actor actionlist 3 { { anim fenrir.stand_zu_kampf } { anim fenrir.kampf_standanim } }
do_action anim fenrir.fifi_zu_fenris 3
do_wait time 0.6
set_particlesource [Object 5] 0 1
sq_object delete 5
do_wait time 3.5
set_visibility [Object 5] 0
start_fade 0.5 0
do_wait time 0.5

#------------------------------------------------------------------------
sq_camera fix Cam06 0.8 -0.1 0.9 4
start_fade 0.5 1
sq_actor actionlist 1 { { anim gesturecloop } { anim gesturee }  { anim gesturecloop } { anim gesturecloop } { anim gesturef } { anim gesturef } { anim gesturee } { anim gesturecloop } { anim gesturee } { anim gesturecloop } { anim gesturecloop } { anim gestureg } { anim gesturegloop } { anim gesturegloop } { anim gesturegloop } { anim gesturegloop } { anim gestureend } { anim gestureendloop } }
do_action anim gesturec 1
do_action beam Text04 2
do_wait time 0.5
sq_wait 2
do_text 001s 2 NoAnim 001s { 60 10 } Off
sq_actor actionlist 0 {}
sq_wait 0
do_text 001t 0 { kingtalkb kingtalka } 001t
sq_camera fix Cam09 0.8 -0.5 0.5
sq_wait 2
do_action beam Text07 2
do_text 001u 2 NoAnim 001u { 40 10 } Off
sq_camera fix Cam06 0.8 -0.1 0.9
sq_wait 0
sq_actor eyes { o o u u l l l o o o c }
sq_actor mouth { c c c 8 8 8 8 8 8 c c }
do_action anim kingtalkb 0
sq_wait 2
sq_actor actionlist 0 { { anim kingsleeploop } loop }
do_action anim kingturnaround 0
do_wait time 1
sq_camera move Cam06 0.65 -0.1 0.9 0.1
do_action beam Text04 2
do_text 001v 2 NoAnim 001v { 50 10 } Off
do_wait time 0.8
sq_wait none
do_text 001w 2 NoAnim 001w { 50 10 } Off
do_wait time 7.5
sq_actor actionlist 0 { { anim kingendloop } loop }
sq_actor express 0 normal_tired
sq_actor mouth 0 { 0 0 10 10 10 10 10 10 10 10 10 10 }
do_action anim kingendstart 0
do_wait time 1.5
sq_actor actionlist 0 {}
sq_actor express 0 normal_tired
sq_wait 0
do_text 001x 0 { kingendtalk } 001x
sq_wait 2
do_text 001y 2 NoAnim 001y Off
sq_wait 0
do_text 001z 0 { kingendtalk } 001z
sq_wait none
sq_actor express 0 good_sleep
sq_actor actionlist 0 { { anim kingendsleeploop } loop }
do_wait time 0.5
do_text 001za 0 { kingendsleepstart } 001za
do_wait time 1.5
start_fade 2 0
do_action beam zurueck 3
+do_wait time 2
+sq_object delete all
sq_camera fix CamEnd 1 0 0
+do_wait time 2

+option set showUI 0

+adaptive_sound markerenable
+adaptive_sound volfact 100






