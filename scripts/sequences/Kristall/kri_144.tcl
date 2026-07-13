#Clip 144 - Zerstören des Lorelei Kristalls
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmokristall
#-----------------------------------------
sq_wait none
sq_camera selset inout
sq_pen set tor [Getobjpos Kristalltor]
sq_pen move tor {0 0 -6}
sq_pen set loreleiunten [Getobjpos Lorelei]
sq_pen set loreleimusicmarker [Getobjpos Lorelei]
sq_pen set loreleioben loreleiunten
sq_pen move loreleioben {-0.2 -10.3 -2.8}
sq_pen set total loreleiunten
sq_pen move total {-7 -5 0}
sq_pen set anfang loreleiunten
sq_pen move anfang {7 -6 0}
sq_pen set ende loreleiunten
sq_pen move ende {-7 -6 0}
sq_pen set von_unten loreleiunten
sq_pen move von_unten {-2 -7 -5}
sq_pen set laserkristall loreleiunten
sq_pen move laserkristall {-0.05 -2.25 -1.8}
sq_pen set laserstart laserkristall
sq_pen move laserstart {15 2 0}
sq_pen set laser laserkristall
sq_pen move laser {20 1.5 -1}
sq_pen set laser1 laserkristall
sq_pen move laser1 {-10 -3 -5}
sq_pen set laser2 laserkristall
sq_pen move laser2 {-3 -10 1}
sq_pen set laser3 laserkristall
sq_pen move laser3 {10 -5 3}
sq_pen set laser4 laserkristall
sq_pen move laser4 {6 -20 2}
sq_pen set laser5 laserkristall
sq_pen move laser5 {-1 -10 3}

sq_pen set p1 loreleioben
sq_pen move p1 {-10 5 0}
sq_pen set p2 loreleioben
sq_pen move p2 {10 -3 0}
sq_pen set p3 loreleioben
sq_pen move p3 {2 -10 0}
sq_pen set p4 loreleioben
sq_pen move p4 {-2 10 0}
sq_pen set p5 loreleioben
sq_pen move p5 {3 10 -1}
sq_pen set p6 loreleioben
sq_pen move p6 {-10 -10 0}
sq_pen set p7 loreleioben
sq_pen move p7 {-10 20 -5}

sq_actor find Lorelei
sq_actor find Kristalltor

+set_anim [Actor 0] kris_lorelei.ganz_standard 0 0
+set_visibility [Actor 1] 0

laserbeam [parse_pos laserkristall] [parse_pos laser] {0 0 0} 18 0 0 1
sq_camera fix anfang 2.5 0.15 0
do_particle create 14 laserkristall {0 -0.05 0} 2 3
do_particle create 14 laserkristall {0 -1 -0.8} 2 3
do_wait time 5
sq_camera fix laserstart 1.1 -0.5 0.7
do_particle create 14 laserkristall {0 -0.05 0} 2 3
do_particle create 14 laserkristall {0 -1 -0.8} 2 3
do_wait time 0.2
sq_camera move laserkristall 1.1 -0.5 0 0.4
do_particle create 14 laserkristall {0 -0.05 0} 2 3
do_particle create 14 laserkristall {0 -1 -0.8} 2 3
do_wait time 2
laserbeam [parse_pos laserkristall] [parse_pos laser1] {0 0 0} 1.5 0.2 0 1
do_wait time 0.5
laserbeam [parse_pos laserkristall] [parse_pos laser2] {0 0 0} 1.4 0 0.2 1
do_wait time 0.4
laserbeam [parse_pos laserkristall] [parse_pos laser3] {0 0 0} 1 0 0 1
do_wait time 0.3
laserbeam [parse_pos laserkristall] [parse_pos laser4] {0 0 0} 0.7 0.2 0.2 1
do_wait time 0.2
laserbeam [parse_pos laserkristall] [parse_pos laser5] {0 0 0} 0.5 0.3 0.1 1
do_wait time 0.5

sq_camera move laserkristall 1.3 -0.3 0 0.4

laserbeam [parse_pos laserkristall] [parse_pos loreleioben] {0 0 0} 11 0 0 1
do_wait time 2

-sound play equake7 1
-sq_screenvibe equake7
sq_camera move von_unten 2 0.5 0.5 0.2;#loreleioben 1.5 0.5 -0.3 0.3
do_particle create 14 loreleioben {0 -0.05 0.3} 125 2
do_wait time 1
do_particle create 14 loreleioben {0 -0.05 0.3} 125 2
do_wait time 1
do_particle create 14 loreleioben {0 -0.05 0.3} 125 2
laserbeam [parse_pos loreleioben] [parse_pos p7] {0 0 0} 0.5 0 0.2 1
do_wait time 1
do_particle create 14 loreleioben {0 -0.05 0.3} 125 2
laserbeam [parse_pos loreleioben] [parse_pos p1] {0 0 0} 0.5 0.2 0.2 1
sq_pen move p1 {0 -5 0}
do_wait time 0.5
laserbeam [parse_pos loreleioben] [parse_pos p2] {0 0 0} 1 0 0 0.5
sq_pen move p1 {0 -5 5}
laserbeam [parse_pos loreleioben] [parse_pos p3] {0 0 0} 0.5 0 0 0.5
do_wait time 0.5
do_particle create 14 loreleioben {0 -0.05 0.3} 125 2
laserbeam [parse_pos loreleioben] [parse_pos p3] {0 0 0} 0.5 0.1 0.1 0.5
sq_pen move p3 {5 0 0}
do_wait time 0.3
laserbeam [parse_pos loreleioben] [parse_pos p4] {0 0 0} 0.6 0 0 0.7
laserbeam [parse_pos loreleioben] [parse_pos p1] {0 0 0} 0.6 0 0.2 0.7
sq_pen move p1 {0 -5 5}
laserbeam [parse_pos loreleioben] [parse_pos p7] {0 0 0} 0.2 0.2 0.2 1
do_wait time 0.3
laserbeam [parse_pos loreleioben] [parse_pos p5] {0 0 0} 0.5 0.2 0 1
laserbeam [parse_pos loreleioben] [parse_pos p3] {0 0 0} 0.4 0.1 0.1 0.8
sq_pen move p3 {0 3 -5}

do_wait time 0.3
do_particle create 14 loreleioben {0 -0.05 0.3} 125 2
laserbeam [parse_pos loreleioben] [parse_pos p6] {0 0 0} 0.5 0.3 0.1 1
do_wait time 0.3
laserbeam [parse_pos loreleioben] [parse_pos p3] {0 0 0} 0.5 0 0.1 0.8
do_wait time 0.3
laserbeam [parse_pos loreleioben] [parse_pos p2] {0 0 0} 0.5 0 0.2 1
do_wait time 0.3
do_particle create 14 loreleioben {0 -0.05 0.3} 125 2
do_wait time 0.5
laserbeam [parse_pos loreleioben] [parse_pos p1] {0 0 0} 0.5 0 0.2 1
laserbeam [parse_pos loreleioben] [parse_pos p7] {0 0 0} 0.5 0.2 0 1
do_wait time 0.5

#sq_camera fix von_unten 1.5 0.5 0.5
-sound play lorelei_knack 1
do_wait time 3
-sound play equake7 1
-sq_screenvibe equake7
do_wait time 1

-sound play lorelei_knall 1
set_anim [Actor 0] kris_lorelei.naseab 0 1
+set_visibility [Actor 1] 0

do_wait time 1.5
sq_camera fix total 2 -0.3 -0.7

do_wait time 2.1
+set_anim [Actor 1] kris_tor.durchbruch 0 0
+set_visibility [Actor 1] 1
+set_anim [Actor 0] kris_lorelei_kris.durchbruch 0 0
+call_method [Getobjref Kristalltor 0] set_open
+call_method [Getobjref Lorelei] free_males

#+set_anim [Actor 2] kris_tor.durchbruch 0 2
sq_pen move tor {-3 -0.8 0}
sq_camera fix tor 1.4 -0.15 0.6
do_wait time 3

sq_camera fix ende 2.5 0.15 0
#sq_camera fix loreleioben 2.3 0.7 0
do_wait time 3
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow kristall
+adaptive_sound marker kristall [parse_pos loreleimusicmarker]
+set colli [obj_query 0 -class Lasercollector -limit 1];if {$colli>0} {adaptive_sound delmarker [get_pos $colli]}
#-----------------------------------------
