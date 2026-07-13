#Clip 190 - Wiggles können nicht beginnen mit Schmieden (zu wenige Ringe)
sq_text file Lava
sq_audio open lav_190
sq_camera selset inout

sq_wait all

sq_pen set amb [get_pos this]
sq_actor find Zwerg 100 1 0 amb

sq_pen set elfePos1 amb
sq_pen move elfePos1 {0 -4 6}
+sq_pen set elfenstart elfePos1
+sq_pen move elfenstart {0 8 0}
+sq_pen set elfenstart2 elfenstart
+sq_pen move elfenstart2 {1 0 0}
sq_pen set elfePos2 amb
sq_pen move elfePos2 {0 -3 0}
sq_pen set elfePos2Kam amb
sq_pen move elfePos2Kam {0 -1.5 0}

sq_camera move elfePos1 0.9 -0.5 0 0.3
do_elf path elfenstart2 elfenstart
do_action anim scratch 0
do_wait time 1
do_elf move elfePos1
do_wait time 2.2
do_elf text 190a {ablehnen} Haltnoch
#"Halt, Stop!"
do_wait time 2.5
sq_camera fix elfePos2 1.3 0.0.5 0
do_elf lookat
do_elf move elfePos2
do_elf text 190b {kopf_schuetteln|reden_a|kopf_schuetteln} Sinddas
do_wait time 5.5
do_elf lookat 0
do_elf text 190c {reden_b|auffordern} Kommversuchs
do_wait time 3.5
do_action rotate front 0
sq_camera fix 0 0.65 0 -0.1
do_wait time 0.3
do_action anim scratchhead 0;#am Spielende ist das fast schon ein running gag :-)
do_wait time 0.7
sq_actor eyes 0 { u u }
do_wait time 0.5
+do_elf hide
