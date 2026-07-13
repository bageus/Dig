#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmourwald
#-----------------------------------------

#--------------- Wiggle kommt ohne Ring der Natur zur Torwächterin -------------------
sq_text file Urwald
sq_audio open urw_040_a
+sq_actor find Zwerg 10 1 0
+sq_actor find Torwaechterin
+set_visibility [Actor 1] 1
+sq_pen set Z1 0
+sq_pen set TW 1
+sq_pen set BT TW


#--- Positionen festlegen ---
+sq_pen move TW {-4.5 0.6 0}
+sq_pen setz TW 10
+sq_pen set T1 TW
sq_pen set C1 T1
+sq_pen move T1 {-5 4.4 0}
+sq_pen setz T1 10
sq_pen move C1 {-3.7 2.5 0}
#+sq_pen move BT {0.2 0.6 3.5}
do_wait

sq_color 0 Wiggle1
sq_color 1 Voodoo1

#--- Torwaechterin positionieren und hinsetzen ---
sq_camera fix Z1 1.2 0.0 0.0
sq_wait none
do_action beam TW 1
do_action rotate left 1
do_wait time 0.5
sq_actor actionlist 1 {loopstart {anim sitedgeloopa} {anim sitedgeloopb} loop}
do_action anim sitedgeloopa 1
sq_camera fix C1 1.3 -0.3 -0.6
sq_pen move C1 {0.2 0 0}

#--- Wiggle geht zu seiner Position ---
sq_wait 0
+do_action run T1 0
do_action rotate 1 0
sq_wait none

#--- Gespräch zwischen Torwaechterin und Wiggle ---
sq_pen move C1 {0.0 -0.6 0}
sq_camera fix C1 0.7 0.1 0.9
do_text 040a_a 1 {sitedgeloopa sitedgeloopb} Undder;# Und der Ring?
do_action anim sitedgeloopa 1
do_wait time 2.0
sq_pen move C1 {1.0 -0.4 0}

sq_camera fix 0 0.65 0.0 0.0
sq_wait 0
do_action rotate front 0
sq_actor eyes 0 {c 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10 10}
do_text 040a_b 0 {scratchhead} Derring;# Der Ring?
sq_wait none

#--- Elfe kommt und spricht mit Wiggle und Torwaechterin geht ins Bett ---
sq_actor actionlist 0 {loopstart {anim standloopa} {anim standloopb} {anim standloopc} {anim standloopd} loop}

sq_pen move C1 {-1.0 0.8 0}
sq_pen set elf_pos_01 C1
sq_pen move C1 {0.8 -0.1 0}
sq_camera fix C1 1.2 0.0 0.0
sq_pen move elf_pos_01 {1.0 -1 0}
sq_pen set elf_pos_02 C1
sq_pen move elf_pos_02 {0 5 0}
sq_pen move TW {4.5 0.0 0}
sq_pen setz elf_pos_01 16
sq_pen setz elf_pos_02 16
do_wait

do_action rotate 5.0 0
sq_wait elf
do_elf path elf_pos_02 elf_pos_01
do_elf lookat 0
sq_wait none
do_elf text 040a_c {} Siebraucht;# Sie braucht den Ring! Bartloser Trottel.
do_wait time 2

#--- Torwaechterin geht ins Bett ---
sq_actor actionlist 1 {}
do_action walk TW 1
do_wait time 3.0
+do_action beam BT 1
+do_action rotate left 1

#--- Elfe spricht weiter
sq_wait elf
do_elf text 040a_d {} Ohnering;# Ohne Ring - geht hier gar nichts
do_elf lookat
do_elf text 040a_e {} Klingtbeinahe;# Klingt beinahe zu einfach, was? Oh, Mann!
+do_elf hide
+set_anim [Actor 1] mann.schlafen_boden_loop 0 2

#--- EndAnimation Wiggle ---
sq_actor actionlist 0 {}
sq_wait 0
do_action rotate front 0
do_action anim breathe 0
sq_wait none

