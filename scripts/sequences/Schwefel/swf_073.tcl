sq_text file Schwefel
sq_audio open 0073
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------
+sq_actor find Zwerg
+sq_actor find Drache
sq_color 1 Drache

do_wait camera
sq_wait all

#################Pens#########################

+sq_pen set DrachenCamPos 1
+sq_pen move DrachenCamPos {3 -2 0}

+sq_pen set WigglePos 1
+sq_pen move WigglePos {-1 0 5.5}

+sq_pen set PumpenPos 1
+sq_pen move PumpenPos {8 -2 0}

##############################################
+do_action walk WigglePos 0
+do_action rotate right 0
sq_wait none
do_wait time 0.5
sq_camera move PumpenPos 1.4 -0.2 -0.1
do_wait time 2
+sq_pen move PumpenPos {2 0 0}
sq_camera move PumpenPos 1.4 -0.2 0.1
do_wait time 2
sq_camera fix DrachenCamPos 1.3 -0.1 -0.6
sq_wait all
do_action anim drache.sitzen_d_ahmann_start 1
do_action anim drache.sitzen_d_ahmann_loop 1
do_action anim drache.sitzen_d_ahmann_loop 1
do_action anim drache.sitzen_d_ahmann_weiter 1
do_text 073a 1 {drache.sitzen_d_ahmann_loop2 drache.sitzen_d_ahmann_loop2 drache.sitzen_d_ahmann_loop2} Es_luegt
do_action anim drache.sitzen_d_ahmann_end 1
do_text 073b 0 talkacpoa Jep_so
do_text 073c 1 {drache.sitzen_dialog_d drache.sitzen_dialog_g drache.sitzen_dialog_d drache.sitzen_dialog_d drache.sitzen_dialog_g} Und_welche
do_text 073d 0 talkpanga Da_haben
sq_camera move DrachenCamPos 1.3 -0.2 -0.2
#sq_camera get
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow schwefelseen
#-----------------------------------------

