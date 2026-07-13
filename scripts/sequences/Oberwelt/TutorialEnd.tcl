sq_text file Tutorial
global actors;foreach gnome $actors {set_particlesource $gnome 1 0;ref_set $gnome current_occupation idle}
sq_wait none
sq_pen set Cam1 TriggerPos
sq_pen move Cam1 {-30 -10 6}
sq_camera fix Cam1 1.5
sq_wait elf
do_elf move Cam1
do_wait time 1
do_elf anim salto
do_elf text 2040k
+sq_camera fix TriggerPos 2.0 -0.15 -0.3
+sq_pen set WalkStart TriggerPos
+sq_pen move WalkStart {-15 0 5}
+sq_pen set WalkDest WalkStart
+sq_pen move WalkDest {20 0 0}
sq_wait none
do_action beam WalkStart 0
do_action walk WalkDest 0
do_wait time 3
+sq_pen move WalkDest {-1 0 0}
do_action beam WalkStart 1
do_action walk WalkDest 1
do_wait time 3
+sq_pen move WalkDest {-1 0 0}
do_action beam WalkStart 2
do_action walk WalkDest 2
do_wait time 3
+sq_pen move WalkDest {-1 0 0}
do_action beam WalkStart 3
sq_wait all
do_action walk WalkDest 3
+do_action beam WalkDest 3
+sq_pen move WalkDest {1 0 0}
+do_action beam WalkDest 2
+sq_pen move WalkDest {1 0 0}
+do_action beam WalkDest 1
+sq_pen move WalkDest {1 0 0}
+do_action beam WalkDest 0
+sq_camera get
do_wait