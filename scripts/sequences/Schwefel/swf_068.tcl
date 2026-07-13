+sq_text file Schwefel
+sq_audio open 0068
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------
+sq_actor find Zwerg 10 1
+sq_actor find Drache
sq_color 1 Drache

do_wait camera
sq_wait all

#################Pens#########################

+sq_pen set DrachenCamPos 1
+sq_pen move DrachenCamPos {3 -2 0}

+sq_pen set WigglePos 1
+sq_pen move WigglePos {-1 0 5.5}

+sq_pen set WigglePos2 1
+sq_pen move WigglePos2 {5 0 3}

+sq_pen set PumpenPos 1
+sq_pen move PumpenPos {8 -2 0}

+sq_pen set HilfsPos PumpenPos
+sq_pen move HilfsPos {1.6  1 -5}
+sq_pen set TittenPos 1
+sq_pen move TittenPos {20 5 0}

##############################################

sq_object summon Stein HilfsPos 0
#+sq_actor find Zwerg 10 3

sq_camera fix DrachenCamPos 1.4 -0.1 -0.6
+do_action walk WigglePos2 0
+do_action rotate 1 0
do_text 068a 0 Auto Hey
#do_action anim drache.sitzen_warten_c 1
do_text 068b 0 Auto Ich_wollte
do_action rotate back 0
do_text 068c 0 Auto Drecksmist
do_action rotate left 0
do_text 068d 0 Auto Ein_bisschen
do_text 068e 1 {drache.sitzen_dialog_g drache.sitzen_dialog_d } Mir_deucht
do_wait time 0.5
do_text 068f 1 {drache.sitzen_dialog_d drache.sitzen_dialog_g drache.sitzen_dialog_g drache.sitzen_dialog_g drache.sitzen_dialog_g} Ja_ja
sq_sound Ha 1
do_action anim drache.sitzen_zu_atem_d_endlich 1
do_text 068g 1 drache.atem_zu_sitzen_d_tretetzurueck Tretet_zurueck
+do_action run WigglePos 0
+do_action rotate PumpenPos 0
sq_wait none
#do_action anim drache.sitzen_zu_pumpe_start 1
do_wait time 1
sq_wait all
call_method [Actor 1] set_enemy [Object 0]
do_action anim drache.sitzen_zu_pumpe_start 1
call_method [Actor 1] fire2_start
sq_sound Pust 1
do_action anim drache.sitzen_zu_pumpe_loop 1
change_particlesource [Object 0] 1 3 {-1.2 0 3} {0 0 0} 156 16 0 5 0 1
set_particlesource [Object 0] 1 1
do_action anim drache.sitzen_zu_pumpe_loop 1
#do_action anim drache.sitzen_zu_pumpe_end 1
do_wait time 1
set_anim [obj_query 0 -class TitanicPumpe] titanic_pumpe.anim 0 2
do_wait time 1


do_wait camera
sq_wait all
sq_camera selset inout
set_anim [obj_query 0 -class TitanicPumpe] titanic_pumpe.anim 0 2
#################
sq_actor find TitanicPumpe
sq_pen set WasserPos 1
sq_pen move WasserPos {27 2 0}
do_wait time 1
+free_particlesource [Object 0] 1 1
call_method [Actor 1] fire_stop
sq_camera move WasserPos 2.3 -0.2 0.2 0.2
do_wait time 6.0
sq_camera move WasserPos 1.5 -0.2 0.3 0.1

do_wait time 6
start_fade 3 0
do_wait time 1
sq_camera fix 0 1.4 -0.2 -0.2
start_fade 3 1



#+sq_camera get
#start_fade 2 1
+sq_object delete all
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow schwefelseen
#-----------------------------------------

