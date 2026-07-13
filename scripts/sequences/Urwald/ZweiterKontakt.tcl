#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmourwald
#-----------------------------------------
sq_audio open 0012
sq_text file Urwald
sq_wait all
do_wait camera
###############################################
sq_pen set Pos1 [Getobjpos Bar]
sq_pen move Pos1 {2 0 8}

sq_pen set Cam1 Pos1
sq_pen move Cam1 {-2 -1 0}

sq_camera move Cam1 0.9 -0.2 0.3
do_wait time 2
do_action walk Pos1 0
do_action rotate 1.1 0
do_text 012a 0 scratchhead Koht
do_wait time 2
sq_camera move Cam1 1.5 -0.1 0.2 0.7
do_wait time 1.5
#sq_camera get

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker peacer [parse_pos Pos1] 40
#-----------------------------------------

