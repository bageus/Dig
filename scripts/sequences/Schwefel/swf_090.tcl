sq_audio open 0090
sq_text file Schwefel
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------
+sq_actor find Drache
+sq_color 1 Drache
sq_wait 1
do_wait camera
do_wait time 2
+sq_pen set DrachenCamPos 1
+sq_pen move DrachenCamPos {0 -2 0}
sq_camera move DrachenCamPos 1.2 0.1 -0.9
#do_action anim drache.sitzen_zu_schulter_l_d_ihredlen 1
do_text 090a 1 { drache.sitzen_zu_schulter_l_d_ihredlen drache.schulter_l_d_warten drache.schulter_l_d_warten } Ihr_seid
do_text 090b 1 {drache.schulter_l_d_warten drache.schulter_l_d_warten drache.schulter_l_d_warten drache.schulter_l_d_warten} Ich_stehe
do_action anim drache.schulter_l_d_ha 1
do_wait time 2
#sq_camera get
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow schwefelseen
#-----------------------------------------

