# Alternatives Ende (II) Start_007
do_wait time 3
set_particlesource [Actor 0] 1 0
sq_actor express 0 good_awake
do_wait time 1
sq_actor actionlist 0 {}
sq_wait 0
do_action anim sleeptostand 0
do_action rotate front 0
do_action anim stretch 0
do_action walkfit P_Start 0
+sq_object delete all
+do_wait

