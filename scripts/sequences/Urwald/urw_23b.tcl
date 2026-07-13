#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmourwald
#-----------------------------------------
sq_text file Urwald
sq_audio open Clip_23
sq_actor find Einsiedler 100 1 any
set_anim /obj/Einsiedler mann.sitzen_sofa_loop 0 2
sq_wait all
#sq_camera follow  0 1.2
sq_pen set ZBack 0
#do_wait camera
#do_wait time 0.6
#do_action rotate left 0
sq_pen set einsiedler_1 1
sq_pen set einsiedler_2 1
sq_pen set zwerg_1 1
sq_pen set zwerg_2 1
sq_pen set kamera 1
sq_pen move zwerg_1 {1.3 0 0.5}
sq_pen move zwerg_2 {2 0 0.5}
sq_pen move einsiedler_2 {-4 0 0}
#sq_camera follow 1 0
#do_wait camera
sq_actor actionlist 0 {{rotate 1}}
sq_actor idleanim 1 {mann.stand_anim_a}

sq_camera fix 1 0.75 0 -0.72
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
do_action run zwerg_1 0
do_action anim couchstop 1
sq_wait all
do_action rotate right 1
#do_text 023a 1 #"Hi"
do_action rotate left 0
sq_object summon Hamster
sq_wait none
link_obj [Object 0] [Actor 0] 0
do_action anim offerjoint 0
do_action anim offerjoint 1
do_wait time 1
link_obj [Object 0] [Actor 1] 0
do_wait time 1
sq_wait all
+sq_object delete all
#do_text 023b 0 {{talkacnga} {talkacnga}} #"So, nun"
#sq_camera fix 1 0.9 -0.4 -0.7
sq_wait none
sq_actor actionlist 0 {{anim talkrepoa} {anim scratch} {anim talkrepoa} {anim standloopa} {anim breathe} {anim standloopb} {anim dontknow} {anim standloopc}}
do_action anim scratchhead 0
do_text 023c 1 {{hungry} {hungry} {washface} {wait}} Meine;#"Meine Hamster"
do_wait time 7
sq_wait all
#sq_wait none
sq_camera fix zwerg_1 0.7 0.0 1.0

#sq_wait all
sq_actor actionlist 0 {{anim listenbloop} {anim listenbloop} {anim listenbloop} {anim listenbloop} {anim listenbstop}}
do_text 023d 1 Auto Wohin;#"Als Dank"
do_action anim listenbstart 0

sq_wait none
sq_actor actionlist 0 {{anim standloopb} {anim scratchhead} {anim kontrol} {anim standloopb} {anim teeter_w} {anim teeter_w} {anim scout} {anim wait} {rotate left}}
sq_actor actionlist 1 {{rotate back} {anim tooltakeout_a} {anim tooltakeout_b} {walk einsiedler_1} {rotate right}}
do_action anim wait 0
sq_wait all
do_action walk einsiedler_2 1
#do_action rotate back 1
#+sq_object delete all
#do_action anim tooltakeout_a 1
#do_action anim tooltakeout_b 1
#sq_camera move einsiedler_1 1.2 -0.2 -0.2
#do_action walk einsiedler_1 1
#sq_wait all
do_action rotate right 1

sq_wait none
sq_actor actionlist 0 {{anim wait} {anim talkrepoa} {anim listenastart} {anim listenaloop} {anim listenaloop} {anim listenaloop} {anim listenaloop} {anim listenaloop} {anim listenastop} {anim wait} {anim wait} {anim talkrepoa} {anim talkrepob} {anim cheer}}
do_text 023e 1 {{getcomfort} {talkacnta} {talkacntb} {getcomfort}} Bei ;#"Ich war"
do_action anim cough 0
do_wait time 8.5

do_text 023f 1 {{talkrentb} {talkpanta} {talkacnta} {talkacngb} {talkacnta} {talkrenta} {talkacnta} {standloopa} {talkacnta} {talkrentb} {talkacnta} {talkacngb} {talkacnta} {talkacnta} {talkacnta}} Lange;#"Ach das"
do_wait time 10
sq_object summon Karte
do_wait time 0.2
do_action anim tooltakeout_a 1
do_wait time 0.3
link_obj [Object 0] [Actor 1] 0
do_action anim tooltakeout_b 1
do_wait time 0.3

sq_wait none
do_action anim offerjoint 1
do_action anim offerjoint 0
do_wait time 1
link_obj [Object 0] [Actor 0] 0
do_wait time 0.5
do_action anim toolputaway_a 0
do_wait time 0.3
do_action anim toolputaway_b 0
do_wait time 0.1
+sq_object delete all
do_wait time 0.2

sq_wait all
sq_camera fix 0 1.1 -0.2 0.1

do_text 023g 0 {cheer} Danke #"Danke"
sq_wait none
do_action rotate 5.4 1
do_wait time 0.5
+do_action beam einsiedler_1 1
do_wait time 1
do_action anim couchstart 1
do_wait time 0.5
sq_wait all
+sq_sound Nichts 0

+set_anim /obj/Einsiedler mann.sitzen_sofa_loop 0 2
+set_roty /obj/Einsiedler 5.4
#+sq_camera get
