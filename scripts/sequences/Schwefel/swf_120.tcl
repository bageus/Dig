#Clip 120 - Erste Tür des Schalterrätsels
sq_wait all
+sq_actor find Tuer_metall 18 3 -1
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------
sq_pen set umdreh 0
sq_pen move umdreh {2 0 4}
sq_pen set die_Tuer 1
sq_pen move die_Tuer {0.5 2 0}
sq_camera selset inout

sq_camera fix 0 1 -0.3 -0.15
#sq_camera get
sq_camera move 1 1.1 -0.18 -0.9
do_wait time 2
#+do_action anim openb 1
+call_method [Actor 1] schalten [get_ref this] -1
+call_method [Actor 2] schliessen
+call_method [Actor 3] schliessen
do_wait time 4
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow titanic
#-----------------------------------------

