+sq_actor find Drache
adaptive_sound changetheme s170
set_textureanimation [Actor 1] 0 1
set_textureanimation [Actor 1] 1 1
sq_audio open 0170
sq_text file Lava
sq_color 1 red
do_wait camera
sq_wait all
sq_pen set WigglePos TriggerPos
sq_pen move WigglePos {1.4035 -4 0}
sq_pen set AwayPos WigglePos
sq_pen move AwayPos {4 0 0}
sq_pen set musicPos TriggerPos
sq_pen move musicPos {-10 0 0}
sq_pen set DP 1
sq_pen move DP {1 -2 0}

sq_pen set HammerPos [Getobjpos Info_Pos_ZwergTmp]
sq_pen move HammerPos {-18.17 0 6.05}

sq_object summon Lava_Hammer_Stein HammerPos
set_roty [Object 0] 3.14

set_roty [Actor 1] 1.57
sq_camera follow 0 1.0
do_action walk WigglePos 0
sq_pen set WigPos 0
sq_pen move WigPos {0 0.2 0}
sq_camera fix WigPos 0.75 -0.2 0.7
do_text 170a 0 scratchhead Was_ist
sq_actor eyes 0 {c c 9 c c c c 9 c c}
do_wait time 2
sq_camera move DP 1.2 0.2 -0.2
do_wait time 1
do_action anim drache.sitzen_warten_b 1
do_wait time 1
do_action anim drache.sitzen_warten_c 1
do_wait time 1
sq_pen set TotalePos 1
sq_pen move TotalePos {6.5 -1 0}
sq_camera fix TotalePos 2.15 -0.2 0.0
do_wait time 1
do_action anim drache.sitzen_warten_c 1
do_wait time 1
do_action anim drache.sitzen_warten_c 1
do_wait time 1
sq_camera fix 0 1.0 -0.2 0.6
do_text 170b 0 Auto Na_wenn


sq_wait none
do_action rotate 4.71 0
do_text 170c 0 Auto Lieber_weg
do_action run AwayPos 0
do_wait time 3
#sq_wait all
sq_camera follow 0 1.0
+adaptive_sound marker drachenhoehle [parse_pos musicPos] 30
+adaptive_sound changetheme drachenhoehle

#+set_roty [Actor 1] 4.71
#+sq_object delete all
