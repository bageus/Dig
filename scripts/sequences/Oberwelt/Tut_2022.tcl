sq_text file Tutorial
sq_audio open 2025a2025f
sq_wait none
sq_pen set Zwerg TriggerPos
sq_pen set Kamera TriggerPos
sq_pen set Elfe TriggerPos
+sq_pen set Stein TriggerPos
sq_pen move Zwerg {18 0 2}
sq_pen move Elfe {1 0 5}
+sq_pen move Stein {2 0 1}
+sq_camera fix Kamera 1.3 -0.1 -0.3
do_elf beam Zwerg
do_elf move Elfe
do_wait time 3
do_elf text 2025a Auto Diese_neu
do_wait time 5
do_elf lookat 0
do_elf anim zeigen_links
do_elf text 2025b Auto Geh_mal
do_wait time 5
sq_wait all
global actors; for {set i 0} {$i<[llength $actors]} {incr i} {do_action run Zwerg $i}

sq_wait elf
do_elf lookat
do_elf text 2025d {reden_a|reden_b} Wenn_du
do_elf text 2025e {reden_a|reden_b} Das_ist
do_elf text 2025i {reden_a|reden_b} Grenzsteine_kannst
do_elf text 2025f {reden_a|reden_b} Warte_ich

sq_wait none
+sq_object summon Grenzstein Zwerg
set_visibility [Object 0] 0
+call_method [Object 0] packtobox
+sq_object beam 0 Stein
elf lookat [Object 0]
do_wait time 1
+set_visibility [Object 0] 1
# hier bitte Partikeleffekte
do_elf lookat none
do_wait time 1
+elf lookat
+sq_camera get
+do_wait
