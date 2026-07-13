# WIGGLES GRABEN BEI VAMPIR-ZWERGEN
adaptive_sound markerdisable
adaptive_sound changetheme atmotourn
sq_wait none
sq_pen set wech TriggerPos
sq_pen move wech { 20 0 0 }
sq_pen set brain1 TriggerPos
sq_pen move brain1 { 1 0 1 }
sq_pen set brain2 TriggerPos
sq_pen move brain2 { -1 0 1 }
sq_pen set deadbrain1 TriggerPos
sq_pen move deadbrain1 { 1 0 1 }
sq_pen set deadbrain2 TriggerPos
sq_pen move deadbrain2 { 2 0 2 }


sq_pen set leanpos TriggerPos
sq_pen move leanpos { 0.5 0 0 }

sq_actor find Zwerg 10 1 3
do_action beam brain1 0
do_action beam brain2 1
sq_actor actionlist { 0 1 } { { { anim scratchhead } { anim calculator } { anim talkacpobq } { anim talkacntcq } { anim talkacntbp } { anim talkacntap } { anim talkacngae } { anim talkrengae } } loop }
do_action rotate 1 0
do_action rotate 0 1
sq_camera selset inout
sq_camera move TriggerPos 1.2 -0.2 0.1
do_wait camera
do_wait time 6
sq_actor actionlist 1 { { wait 0.6 } { anim showright } }
sq_actor actionlist 0 {}
do_action rotate front 1
sq_wait 0
do_action anim talkrengbp 0
do_action anim talkrengae 0
sq_wait { 1 0 }
sq_object summon Presslufthammer wech
do_action rotate front 0
sq_wait none
do_action anim dontknow 0
sq_wait 1
do_action anim tooltakeout_a 1
sq_wait { 1 0 }
do_action anim tooltakeout_b 1
sq_wait none
sq_actor actionlist 1 { { anim airhammdownloop } loop }
do_action anim airhammdownstart 1
sq_wait 0
do_action rotate right 0
do_action anim bowllose 0
do_action walk leanpos 0
do_action rotate front 0
do_action anim leanstart 0
do_action anim leanloop 0
sq_actor actionlist 1 {}
sq_wait none
sq_actor actionlist 0 { { anim leanloop } { anim shock } { rotate back } { rotate front } { rotate right } { anim standbackhith } }
do_action anim leanloop 0
start_fade 2 0
do_action anim airhammdownaccident 1
do_wait time 0.4
set_vel [Actor 1] { 0.5 0 -0.2 }
sq_wait 1
do_wait
set_vel [Actor 1] {0 0 0}
sq_wait none
-sound play pressl_start 1
do_wait time 0.6
set_vel [Actor 1] {-0.7 0 0}
do_wait time 0.5
set_vel [Actor 1] {0 0 0}
do_wait time 2
sq_actor actionlist { 1 0 } {}
sq_wait 1
do_wait
+do_action beam deadbrain1 1
+do_action beam deadbrain2 0
+set_activegameplay [Actor 1] 0
+set_activegameplay [Actor 0] 0
+link_obj [Object 0] [Actor 1] 0
+set_anim [Actor 1] mann.fallen_end_tot 10 0
+set_anim [Actor 0] mann.fallen_end_tot 10 0
+set_roty [Actor 0] 1.8
+set_roty [Actor 1] 3.2
+set_textureanimation [Actor 0] 4 9
+set_textureanimation [Actor 1] 4 9
do_wait time 5
start_fade 2 1
do_wait time 2
+adaptive_sound markerenable
+adaptive_sound changetheme tournament
+cancel_fade






