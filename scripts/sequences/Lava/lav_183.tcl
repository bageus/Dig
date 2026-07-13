#Clip 183 "Erstes Betreten des Hammerraums, aber kein Rankommen"(Kamerafahrt übern Vulkan)

sq_text file Lava
sq_audio open lav_183
sq_camera selset inout
+sq_wait all

sq_pen set anfang TriggerPos
sq_pen set amb [Getobjpos Amboss]
sq_pen set elfeUnten amb
sq_pen move elfeUnten {3 -2 0}
sq_pen set elfeWegpunkt amb
sq_pen move elfeWegpunkt {3 -7 10}

sq_pen set krater amb
sq_pen move krater {-1 -20 0}
sq_pen set elfeOben krater
sq_pen move elfeOben {2 -4 3}
sq_pen set elfeStart elfeOben
sq_pen move elfeStart {2 -4 3}

#+sq_camera get
#do_wait time 0.2
sq_camera move 0 1 0 0.5 0.5
do_elf path elfeStart elfeOben
do_wait time 1
do_action rotate amb 0
do_wait time 0.5
do_action anim lookup 0
do_wait time 0.5
sq_camera move krater 1.7 -0.7 0.1 0.4
do_wait time 3.5
do_elf lookat 0
sq_wait elf
do_wait time 0.5
do_elf text 183a {reden_b} Nadas
do_elf hide
#do_elf path elfeOben elfeWegpunkt
do_wait time 0.5
sq_camera move amb 1.5 -0.1 0.7 0.3
do_wait time 1
do_elf lookat
do_wait time 0.5
do_elf path elfeWegpunkt elfeUnten
do_wait time 3
do_elf text 183b {reden_a|kopf_schuetteln} Meineguete
do_wait time 0.5
+do_elf hide

#das wars schon
