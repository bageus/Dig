do_wait camera
sq_wait all
sq_camera selset inout
###########################################
#sq_actor find Zwerg 50 1 0 TriggerPos
sq_actor find TitanicPumpe

sq_actor find Kohle 5 1



sq_pen set Pumpe 1
sq_pen set PumpePos Pumpe
sq_pen set k1 2
#sq_pen set k2 3
#sq_pen set k3 4
sq_camera move Pumpe 1 -0.2 0.5 0.2
do_wait camera
sq_wait 0
sq_pen move Pumpe {-2.5 0.0 2.5}
do_action walk k1 0
do_action anim bend 0
link_obj [Actor 2] [Actor 0] 0
do_action transport Pumpe 0
do_action rotate back 0
+link_obj [Actor 2]
set_visibility [Actor 2] 0
#+sq_object delete 2

do_action anim putjump 0
do_wait time 0.5
##
#do_action walk k2 0
#do_action anim bend 0
#link_obj [Actor 3] [Actor 0] 0
#do_action transport Pumpe 0
#do_action rotate back 0
#+link_obj [Actor 3]
#set_visibility [Actor 3] 0
#+sq_object delete 3

#do_action anim putjump 0
#do_wait time 0.5
##
#do_action walk k3 0
#do_action anim bend 0
#link_obj [Actor 4] [Actor 0] 0
#do_action transport Pumpe 0
#do_action rotate back 0
#+link_obj [Actor 4]
#set_visibility [Actor 4] 0
#do_action anim putjump 0
#do_wait time 1

#sq_pen set FIREPOS  PumpePos
#sq_pen move FIREPOS {0 -2 0}
#do_particle create 0 FIREPOS {0 0 1} 5 5


###########################################
+set_anim [obj_query 0 -class TitanicPumpe] titanic_pumpe.anim 0 2
#################

sq_pen set WasserPos 1
sq_pen move WasserPos {27 2 0}
do_wait time 2
sq_camera move WasserPos 2.3 -0.2 0.2 0.2
do_wait time 4.0
sq_camera move WasserPos 1.5 -0.2 0.3 0.1

do_wait time 6
start_fade 3 0
do_wait time 1
sq_camera fix 0 1.4 -0.2 -0.2
do_wait time 2
start_fade 3 1
do_wait time 3
+del [obj_query 0 -class Trigger_swf_unq_Pumpitup]
+del [obj_query 0 -class Trigger_Swf_073]
+foreach item [obj_query [Getobjref TitanicPumpe] -class Kohle -range 5 -limit 30] {log "Item: $item: [get_objname $item] wird geloescht"; del $item}
