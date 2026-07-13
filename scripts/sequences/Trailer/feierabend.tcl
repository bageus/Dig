sq_wait camera
sq_camera fix {35.7 33.2 14} 0.7 -0.2 -0.1
sq_pen set Bar TriggerPos
sq_pen set Wachhaus [get_pos [obj_query this "-class Wachhaus -limit 1 -range 1000 -owner 0"]]
sq_pen set Kueche [get_pos [obj_query this "-class Primitivkueche -limit 1 -range 1000 -owner 0"]]
sq_pen set Lift [get_pos [obj_query this "-class Aufzug -limit 1 -range 1000 -owner 0"]]
set_sequenceactive [obj_query this "-class Aufzug -limit 1 -range 1000 -owner 0"] 1
sq_pen set Hammer [get_pos [obj_query this "-class Dampfhammer -limit 1 -range 1000 -owner 0"]]
set_anim [obj_query this "-class Dampfhammer -limit 1 -range 1000 -owner 0"] dampfhammer.ani 0 2
sq_pen set Farm [get_pos [obj_query this "-class Farm -limit 1 -range 1000 -owner 0"]]
sq_pen set Klotz [get_pos [obj_query this "-class Hauklotz -limit 1 -range 1000 -owner 0"]]
sq_pen set VR Bar
sq_pen set VO Lift
sq_pen set VL Wachhaus
sq_pen set B0 Bar
sq_pen set B1 Bar
sq_pen set B2 Bar
sq_pen set B3 Bar
sq_pen set B4 Bar
sq_pen set T0 Bar
sq_pen set T1 Bar
sq_pen set Farm1 Farm
sq_pen set Farm2 Farm
sq_pen move B0 {-0.3 0 2.4}
sq_pen move B1 {2.1 0 1.}
sq_pen move B2 {2.85 0 5.5}
sq_pen move B3 {-1.5 0 2}
sq_pen move B4 {-3.9 0 1.9}
sq_pen move T0 {-1 0 6.2}
sq_pen move T1 {-0.2 0 7}
sq_pen move Kueche {0 0 1.5}
sq_pen move Wachhaus {0 0 2}
sq_pen move Farm {-1 0 1}
sq_pen move Farm1 {1 0 0}
sq_pen move Farm2 {0 0 -1}
sq_pen move Klotz {0.7 0 0.7}
sq_pen move VL {-6.5 -8 0}
sq_pen setz VL 14
sq_pen move VR {15 0 0}
sq_pen setz VR 13
sq_pen move VO {3 2 0}
sq_pen setz VO 13
sq_pen move Hammer {0 0 2}
do_action beam B0 0
do_action beam B1 1
do_action beam B2 2
do_action beam B3 3
do_action beam B4 4
do_action beam T0 5
do_action beam T1 6
do_action beam Kueche 7
do_action beam Wachhaus 8
do_action beam Farm 9
do_action beam Klotz 10
do_action beam VL 11
do_action beam Wachhaus 12
do_action walk VO 12
sq_pen move B3 {-0.5 0 0.4}
sq_pen move B2 {-1.1 0 -2.2}
sq_pen move VL {5.8 8 0}
global actors; set_roty [lindex $actors 0] 0.4
global actors; set_roty [lindex $actors 1] 5.5
global actors; set_roty [lindex $actors 2] 2
global actors; set_roty [lindex $actors 3] 1
global actors; set_roty [lindex $actors 4] 4.2
global actors; set_roty [lindex $actors 5] 5.1
global actors; set_roty [lindex $actors 6] 2
global actors; set_roty [lindex $actors 7] 3.14
global actors; set_roty [lindex $actors 8] 0
global actors; set_roty [lindex $actors 9] 4
global actors; set_roty [lindex $actors 10] 2.5
global actors; set_roty [lindex $actors 11] 4.71
sq_actor actionlist 0 {{walk B3} {rotate 1.9} {anim bend} {anim wait} {walk B2} {rotate 4.5} {anim kontrol} {walk B0} {rotate 0.4} {anim barkeeper} {walk B2} {rotate 4.5} {anim bend} {anim wait} {walk B0}}
sq_actor actionlist 1 {{anim sitchairloop} {anim sitchairloop} {anim sitchairbore} {anim sitchairloop} loop}
sq_actor actionlist 2 {{anim sitchairorder} {anim sitchairloop} {anim sitchairloop} {anim sitchairloop} loop}
sq_actor actionlist 3 {{anim sitchairloop} {{anim sitchairbore} {anim sitchairloop}} {anim sitchairloop} {anim sitchairloop} {anim sitchairdrink} {anim sitchairloop} loop}
sq_actor actionlist 4 {{anim sitchairloop} {anim sitchairloop} {anim sitchairloop} {anim sitchairloop} {anim sitchairdrink} loop}
sq_actor actionlist 5 {{anim discoa} {anim discoc} {anim discod} {rotate right} {anim discod} {anim discod} {rotate 6} loop}
sq_actor actionlist 6 {{anim discod} {anim discoa} {anim discoc} {rotate left} {anim djhigh} {anim discoa} {anim discoc} {rotate 5} loop}
sq_actor actionlist 7 {{anim stirkettle} {{anim stirkettle} {anim stir}} {anim wait} {{anim breathe} {anim read} {anim tired}} {anim workatfire} loop}
sq_actor actionlist 8 {{{rotate 5.3} {rotate 4.1} {rotate right}} {anim guardwalk} {rotate front} {{anim wait} {anim tired} {anim teeter_w}} loop}
sq_actor actionlist 9 {{anim bend} {walk Farm1} {rotate 2.5} {anim bend} {walk Farm2} {rotate 6.1} {anim bend} {walk Farm} {rotate 4.0} loop}
sq_actor actionlist 10 {{anim hammerloop} {anim hammerloop} {{anim hammerloop} {anim workatfire} {anim tired} {anim cough}} loop}
do_action anim barkeeper 0
do_action anim sitchairloop {1 2 3 4}
do_action anim discoa {5 6}
do_action anim stir 7
do_action anim wait 8
do_action anim bend 9
do_action anim hammerloop 10
do_wait time 2
sq_camera move {35.5 33 14} 0.9 -0.25 -0.25 0.15
do_wait time 4
sq_camera move {35.3 32.8 14} 1 -0.3 -0.35 0.15
do_wait time 4
sq_pen move B3 {0.2 0 2.2}
sq_pen move B4 {1 0 3.2}
sq_actor actionlist 3 {{anim standup_chair} {walk B3} {rotate 1.3}}
sq_actor actionlist 4 {{anim standup_chair} {walk B4} {rotate 4.0}}
sq_camera move {36 31.5 14} 1.2 -0.3 -0.45 0.15
do_wait time 4
sq_actor actionlist 3 {{anim discoa} {anim discoc} {anim discod} {rotate left} {anim discod} {anim discod} {rotate 4} loop}
sq_actor actionlist 4 {{anim discod} {anim discoa} {anim discoc} {rotate right} {anim djhigh} {anim discoa} {anim discoc} {rotate 3} loop}
sq_camera move {37 30 14} 1.4 -0.25 -0.45 0.15
do_action walk VL 11
do_wait time 5
sq_camera move {39.5 28 14} 1.6 -0.2 -0.35 0.15
do_action beam VR 12
sq_wait 11
do_wait
# sq_pen move VL {4.5 0 0}
# do_action walk VL 11
sq_actor actionlist 8 {{rotate 0.7} {anim talkh}}
do_action rotate 3.9 11
do_action anim talkh 11
sq_wait none
sq_actor actionlist 8 {{{rotate 5.6} {rotate 3.9} {rotate right}} {anim guardwalk} {rotate front} {{anim wait} {anim tired} {anim teeter_w}} loop}
do_action walk VR 11
sq_camera move {40.5 27 14} 1.9 -0.15 -0.25 0.15
do_wait time 5
sq_camera move {42 26 14} 2.6 -0.2 -0.2 0.15
do_wait time 5
sq_camera move {43 25 14} 4 -0.3 -0.15 0.13
do_action walk Hammer 12
do_wait time 17
set_anim [obj_query this "-class Dampfhammer -limit 1 -range 1000 -owner 0"] dampfhammer.standard 0 2
global viewpos; set viewpos [get_view]
do_wait
