#Clip 131 - Zwerge erhalten 2. Ring!
sq_wait all
+sq_actor find Schatzkiste
+sq_pen set die_Kiste 1
+sq_pen set vor_Kiste 1
+sq_pen move vor_Kiste {0 0 2}
sq_pen set weggeh vor_Kiste
sq_pen move weggeh {2 0 5}
sq_pen set wegzeig vor_Kiste
sq_pen move wegzeig {1 0 2.5}
sq_camera selset inout

sq_camera fix vor_Kiste 0.9 -0.2 0.25
do_action walk vor_Kiste 0
do_action rotate back 0

do_wait time 1
do_tooltakeout Axt 0
do_action anim hackstart 0
do_action anim hackloop 0
do_action anim hackend 0
do_action anim hackqustart 0
do_action anim hackquloop 0
do_action anim hackquend 0

do_wait time 2
do_action anim hackqustart 0
do_action anim hackquloop 0
do_action anim hackquend 0
do_action anim hackstart 0
do_action anim hackloop 0
do_action anim hackend 0
do_action anim standloopb 0
do_action anim standloopb 0
sq_camera fix vor_Kiste 0.7 -0.2 -0.3
do_action anim scratchhead 0
do_action anim standloopb 0
do_toolputaway 0
do_action anim kickmachine 0
do_wait time 1.5
do_action anim scratchhead 0
sq_actor express 0 bad_dizzy
gametime factor 2
sq_camera move vor_Kiste 0.8 -0.2 0.2 0.3
do_action anim kickmachine 0
do_action anim kickmachine 0
do_action anim kickmachine 0
gametime factor 1
do_wait time 1.5
do_action rotate right 0
do_action anim dontknow 0

do_action rotate weggeh 0
sq_wait none
sq_camera move wegzeig 0.8 -0.2 0.2 0.3
do_action walk weggeh 0
do_wait time 3
+sq_camera fix die_Kiste 1 -0.05 0.0
sound play tuer_oeffnen 1
+call_method [Actor 1] release_content 0              ;#Ring wird freigelegt,
+sq_actor find Ring_Des_Wassers 30 1 any die_Kiste   ;#dann wird er aufgegabelt
call_method [Actor 2] setstandardanim              ;#die Animation angehalten
+do_action beam die_Kiste 2                           ;#und zur Kiste getan

do_wait time 1
sq_wait all

sq_actor express 0 good_normal
do_action rotate die_Kiste 0
do_action run vor_Kiste 0
do_action rotate back 0
sq_wait none
do_action anim offerjoint 0
do_wait time 1
link_obj [Actor 2] [Actor 0] 0
#---inserted by Jan----------
sound play equake7 1
sq_screenvibe equake7
#----------------------------
do_wait time 1
sq_wait all
+do_action rotate front 0
do_action anim swordupstart 0
do_action anim sworduploop 0
do_action anim swordupend 0
do_action anim cheer 0

#---inserted by Jan----------
start_fade 1 0
do_wait time 1
#----------------------------
set trig [obj_query 0 -class Trigger_Fenris_131a -limit 1]; if {$trig>0} {call_method $trig activate_fenris_131a} else { cancel_fade }

+sq_object delete all
+link_obj [Actor 2]
+do_action beam vor_Kiste 0
+call_method [Actor 2] reset
+ringuebergeben [Actor 0]
#in der letzten Zeile kriegt ers ins Inventory

