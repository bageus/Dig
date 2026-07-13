sq_text file Schwefel
sq_audio open 0071
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------
+rausbuddel
+sq_actor find Drache
+sq_color 1 Drache

+set_collision [Actor 1] 1

do_wait camera
sq_wait all

#################Pens#########################

+sq_pen set DrachenCamPos 1
+sq_pen move DrachenCamPos {-2 -2 0}

+sq_pen set WiggleCamPos 1
+sq_pen move WiggleCamPos {-4 0 0}

+sq_pen set WigglePos 1
+sq_pen move WigglePos {-5 0 1}

+sq_pen set WiggleBeamPos WigglePos
+sq_pen move WiggleBeamPos {9 0 0}

+sq_pen set DrachenBeamPos [Getobjpos Info_Drache_Waypoint2 100 any]

+sq_pen set WiggleSwervePos WigglePos
+sq_pen move WiggleSvervePos {0 0 -4}


##############################################
sq_camera fix DrachenCamPos 1.4 -0.2 0.2
do_wait time 1
+do_action beam WiggleBeamPos 0
+do_action run WigglePos 0
+do_action rotate 1 0
do_text 071a 0 Auto Wir_sind
do_action anim drache.sitzen_zu_schulter_r 1
do_action anim drache.schulter_r_warten 1
sq_wait none
do_text 071b 1 {drache.schulter_r_warten drache.schulter_r_warten drache.schulter_r_warten drache.schulter_r_warten drache.schulter_r_d_willman} Endlich_hab
do_wait time 10
sq_camera fix WiggleCamPos 0.65 -0.1 -0.64
+do_action beam DrachenBeamPos 1
do_text 071c 0 Auto So_isses
do_wait time 1
do_action anim drache.sitzen_gehen 1
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt2 1
do_wait time 1
do_text 071d 0 {talkpapoa mann.dialog_re_neutral_b mann.dialog_pa_negativ_c mann.dialog_ac_positiv_a} Was_die
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt1 1
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt2 1
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt1 1
sq_wait all
do_action anim talkpapoa 0
sq_actor mouth 0 {5}
+set_roty [Actor 1] 4.71
do_wait time 6
sq_camera follow 0 1.3

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow schwefelseen
#-----------------------------------------

