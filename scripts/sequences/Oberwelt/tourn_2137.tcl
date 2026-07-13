# WIGGLES FERTIG MIT BRAUEN
#elf say "Sequenz tourn-2137"

#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmotourn
#-----------------------------------------

sq_wait none
sq_pen set Zwerg0 TriggerPos
sq_pen move Zwerg0 {-2 0 1.5}
#sq_pen set Schalterpos [call_method [obj_query this "-class Schalter_hebel_holz_up -range 15 -limit 1"] get_switchpos]
#sq_pen move Schalterpos {-2 0 2}
sq_pen set Bierpos [get_pos [obj_query this "-class Dummy_Holzstuhl_b -range 15 -limit 1"]]
sq_pen set Kamera2 Bierpos
sq_pen set Drunkpos Bierpos
sq_pen move Drunkpos {3 0 0}
do_action beam Drunkpos 1
sq_pen move Drunkpos {-1.5 0 -1.5}
do_action rotate 1 1
sq_pen move Bierpos {0.1 0 1}
do_action beam Bierpos 0
sq_pen move Bierpos {-0.12 0 -0.55}
sq_pen move Kamera2 {0.5 -0.6 2.5}
sq_pen set Kamera1 Kamera2
sq_pen move Kamera1 {-5 -0.3 0}
sq_camera fix Kamera1 1 0.1 -0.66
do_action beam Zwerg0 2
sq_pen move Zwerg0 {2 0 -0.5}
sq_wait 0
do_action beam Bierpos 0
do_action rotate front 0
sq_camera move Kamera2 0.96 -0.25 0.25 0.1
sq_wait 2
do_action walk Zwerg0 2
global actors; set_anim [lindex $actors 0] mann.trinken_fass_loop 0 2
change_particlesource [obj_query this "-class Dummy_Holzstuhl_b -range 15 -limit 1"] 0 15 {0 -0.7 0.9} {0 0 0} 200 2 1
set_particlesource [obj_query this "-class Dummy_Holzstuhl_b -range 15 -limit 1"] 0 1
do_action rotate back 2
do_action anim drinktubstart 2
global actors; set_anim [lindex $actors 2] mann.bottich_trinken_loop 0 2
#global actors; set_roty [lindex $actors 1] 3.14
#sq_wait 0
do_wait
#do_action anim drinkbarrelstart 0
do_wait time 2
sq_wait 1
do_action walk Drunkpos 1
global actors; set_anim [lindex $actors 1] mann.schlafwandeln 2 1
sq_wait camera
do_wait time 2
global viewpos; set viewpos [get_view]
do_wait time 5
set_particlesource [obj_query this "-class Dummy_Holzstuhl_b -range 15 -limit 1"] 0 0
do_wait
+adaptive_sound changetheme tournament

