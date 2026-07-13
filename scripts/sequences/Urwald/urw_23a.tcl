gametime factor 1
sq_text file Urwald
sq_audio open Clip_22
sq_actor find Einsiedler 100 1 any
set_anim /obj/Einsiedler mann.sitzen_sofa_loop 0 2
sq_wait all
#sq_camera follow  0 1.2
#do_wait camera
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }

do_action rotate left 0
sq_pen set abgeben 1
sq_pen move abgeben {1.2 0 0.5}
sq_pen set mitte 1
sq_pen move mitte {0.6 0 0}
sq_actor idleanim 1 {mann.stand_anim_a}

sq_wait none
do_action anim couchstop 1
sq_camera fix mitte 0.8 0.1 0.4
sq_wait all

sq_actor actionlist 0 {{rotate 1}}
do_action run abgeben 0
do_action rotate 0 1

#do_text 023a 1 {{talkrepoa} {talkacnta}}
do_text 023h 0 {talkacntb} HierIch

sq_object summon Hamster
sq_wait none
link_obj [Object 0] [Actor 0] 0
do_action anim offerjoint 0
do_action anim offerjoint 1
do_wait time 0.5
link_obj [Object 0] [Actor 1] 0
do_wait time 1

+sq_object delete 0

do_wait time 1.2
do_text 023i 1 {{cheer} {hungry}} Fuer
do_action anim scratchhead 0
do_wait time 5
sq_wait none
sq_actor actionlist 0 {{anim talkrepoa} {anim standloopa}}
do_action anim scratch 0
do_text 023j 0 {{showup} {talkacnta} {talkacntb}} OhAlter
do_wait time 1

sq_wait none
sq_camera move 0 1.9 -0.2 -0.2 0.4
do_wait time 1.2
do_action anim couchstart 1
do_wait time 0.2

sq_wait all
do_action rotate 5.4 1
+set_anim /obj/Einsiedler mann.sitzen_sofa_loop 0 2
+set_roty /obj/Einsiedler 5.4
do_wait time 0.2
#+sq_camera get
