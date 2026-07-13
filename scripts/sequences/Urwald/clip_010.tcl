#CLIP 10 Feen übergeben magisches Schwert, als Belohnung für das Wasser.
#Trigger_Urw_unq_feen_End

sq_text file Urwald
sq_audio open Clip_10

+sq_pen set Rechts [Getobjpos Info_Pos_ZwergTmp]

+sq_pen set Rechtscam Rechts
+sq_pen move Rechtscam {-2 0 -1}

+sq_pen set RechtsZwerg Rechts
+sq_pen move RechtsZwerg {1.6 1.3 5}

sq_actor find Info_Pos_ZwergTmp 20 1

+sq_pen set feeZwerg RechtsZwerg
+sq_pen move feeZwerg {0 -0.8 0}

+sq_pen set feen Rechts
+sq_pen move feen {4 0 -4}

+sq_pen set Wasser RechtsZwerg
+sq_pen move Wasser {-5 0 -4}

+sq_pen set WasserZwerg Rechts
+sq_pen move WasserZwerg {-3 2 1}

+sq_pen set WasserBoden RechtsZwerg
+sq_pen move WasserBoden {-5 0.7 -4}

+sq_pen set Wasserfee Wasser
+sq_pen move Wasserfee {0 -0.5 0}

+sq_pen set Wasserfee2 Wasserfee
+sq_pen move Wasserfee2 {2 0 0}

+sq_pen set Wassercam Wasser
+sq_pen move Wassercam {-0.2 0 2}

+sq_pen set WasserfallPos Wasser
+sq_pen move WasserfallPos {0 0 0}

+sq_pen set Mitte [Getobjpos Info_Pos_Troll]

+sq_pen set Mittecam Mitte
+sq_pen move Mittecam {5 0 0}

+sq_pen set AufbluehenCam Rechts
+sq_pen move AufbluehenCam {-2 0 0}

+gametime factor 1
#+do_fee back

sq_wait 0
sq_camera fix 0 1.08

#do_action rotate WasserfallPos 0
do_action rotate Wasser 0

+sq_pen set BaumPos AufbluehenCam
+sq_pen move BaumPos {0 0 0}
+sq_pen set Baum1Pos BaumPos
+sq_pen move Baum1Pos {-3 -1.5 3.7}
+sq_pen set Baum2Pos BaumPos
+sq_pen move Baum2Pos {-0.5 -0.5 -3}
+sq_pen set Baum3Pos BaumPos
+sq_pen move Baum3Pos {3 -0.2 -4}
+sq_pen set Baum4Pos BaumPos
+sq_pen move Baum4Pos {2.4 -1 -4}
+sq_pen set Baum5Pos BaumPos
+sq_pen move Baum5Pos {5 -2 3}
+sq_pen set Baum6Pos BaumPos
+sq_pen move Baum6Pos {7 -0.7 1.5}
+sq_pen set Baum7Pos BaumPos
+sq_pen move Baum7Pos {5 -1.7 2.7}
+sq_pen set Baum8Pos BaumPos
+sq_pen move Baum8Pos {3 -2.1 3.8}
+sq_pen set Baum9Pos BaumPos
+sq_pen move Baum9Pos {5.0 -1.3 -1.5}
+sq_pen set Baum10Pos BaumPos
+sq_pen move Baum10Pos {7.0 -0.8 -4}
+sq_pen set Baum11Pos BaumPos
+sq_pen move Baum11Pos {3.0 -1.8 -2.0}
+sq_pen set Baum12Pos BaumPos
+sq_pen move Baum12Pos {1.0 -1.8 2.0}
+sq_pen set Baum13Pos BaumPos
+sq_pen move Baum13Pos {1 1 -6}
+sq_pen set Baum14Pos BaumPos
+sq_pen move Baum14Pos {8 -2 1}


#sq_pen set BaumPos 0
do_particle create 14 Baum1Pos {0.2 -0.2 0.03} 2 2
do_particle create 14 Baum2Pos {-0.3  -0.1 0.05} 3 2
do_particle create 14 Baum3Pos {-0.3 -0.2 0.05} 10 2
do_particle create 14 Baum4Pos {-0.1 -0.1 -0.3} 6 2
do_particle create 14 Baum5Pos {-0.2 -0.1 0.01} 2 2
do_particle create 14 Baum6Pos {0.2  -0.1 0.01} 2 2
do_particle create 14 Baum7Pos {0.2  -0.2 0.2} 2 2
do_particle create 14 Baum8Pos {0.2   0.2 0.01} 2 2
do_particle create 14 Baum9Pos  {0.1 0.1 0.1} 2 2
do_particle create 14 Baum10Pos {-0.1 -0.1 0.01} 2 2
do_particle create 14 Baum11Pos {-0.1 -0.1 0.01} 2 2
do_particle create 33 Baum12Pos {-0.1 -0.1 0.05} 3 2
do_particle create 33 Baum13Pos {-0.1 -0.1 -0} 3 2
do_particle create 33 Baum14Pos {0.1 0.1 0.01} 2 2
do_action anim scout 0

sq_camera fix AufbluehenCam 1.28 0.02 0.59
sq_wait none
do_action walk WasserZwerg 0
do_particle create 14 Baum1Pos {0.2 -0.2 0.03} 2 2
do_particle create 14 Baum2Pos {-0.3  -0.1 0.05} 3 2
do_particle create 14 Baum3Pos {-0.3 -0.2 0.05} 10 2
do_particle create 14 Baum4Pos {-0.1 -0.1 -0.3} 6 2
do_particle create 14 Baum5Pos {-0.2 -0.1 0.01} 2 2
do_fee move Wasserfee
do_particle create 14 Baum6Pos {0.2  -0.1 0.01} 2 2
do_particle create 14 Baum7Pos {0.2  -0.2 0.2} 2 2
do_particle create 14 Baum8Pos {0.2   0.2 0.01} 2 2
do_particle create 14 Baum9Pos  {0.1 0.1 0.1} 2 2
do_particle create 14 Baum10Pos {-0.1 -0.1 0.01} 2 2
do_particle create 14 Baum11Pos {-0.1 -0.1 0.01} 2 2
do_particle create 33 Baum12Pos {-0.1 -0.1 0.05} 3 2
do_particle create 33 Baum13Pos {-0.1 -0.1 -0} 3 2
do_particle create 33 Baum14Pos {0.1 0.1 0.01} 2 2

do_particle create 14 Baum1Pos {0.2 -0.2 0.03} 2 2
do_particle create 14 Baum2Pos {-0.3  -0.1 0.05} 3 2
do_particle create 14 Baum3Pos {-0.3 -0.2 0.05} 10 2
do_particle create 14 Baum4Pos {-0.1 -0.1 -0.3} 6 2
do_particle create 14 Baum5Pos {-0.2 -0.1 0.01} 2 2
do_particle create 14 Baum6Pos {0.2  -0.1 0.01} 2 2
do_particle create 14 Baum7Pos {0.2  -0.2 0.2} 2 2
do_particle create 14 Baum8Pos {0.2   0.2 0.01} 2 2
do_particle create 14 Baum9Pos  {0.1 0.1 0.1} 2 2
do_particle create 14 Baum10Pos {-0.1 -0.1 0.01} 2 2
do_particle create 14 Baum11Pos {-0.1 -0.1 0.01} 2 2

do_particle create 14 Baum1Pos {0.2 -0.2 0.03} 2 2
do_particle create 14 Baum2Pos {-0.3  -0.1 0.05} 3 2
do_particle create 14 Baum3Pos {-0.3 -0.2 0.05} 10 2
do_particle create 14 Baum4Pos {-0.1 -0.1 -0.3} 6 2
do_particle create 14 Baum5Pos {-0.2 -0.1 0.01} 2 2
do_particle create 14 Baum6Pos {0.2  -0.1 0.01} 2 2
do_particle create 14 Baum7Pos {0.2  -0.2 0.2} 2 2
do_particle create 14 Baum8Pos {0.2   0.2 0.01} 2 2
do_particle create 14 Baum9Pos  {0.1 0.1 0.1} 2 2
do_particle create 14 Baum10Pos {-0.1 -0.1 0.01} 2 2
do_particle create 14 Baum11Pos {-0.1 -0.1 0.01} 2 2

do_wait time 1.3

do_particle create 14 Baum1Pos {0.2 -0.2 0.03} 2 2
do_particle create 14 Baum2Pos {-0.3  -0.1 0.05} 3 2
do_particle create 14 Baum3Pos {-0.3 -0.2 0.05} 10 2
do_particle create 14 Baum4Pos {-0.1 -0.1 -0.3} 6 2
do_particle create 14 Baum5Pos {-0.2 -0.1 0.01} 2 2
do_particle create 14 Baum6Pos {0.2  -0.1 0.01} 2 2
do_particle create 14 Baum7Pos {0.2  -0.2 0.2} 2 2
do_particle create 14 Baum8Pos {0.2   0.2 0.01} 2 2
do_particle create 14 Baum9Pos  {0.1 0.1 0.1} 2 2
do_particle create 14 Baum10Pos {-0.1 -0.1 0.01} 2 2
do_particle create 14 Baum11Pos {-0.1 -0.1 0.01} 2 2

sq_camera selset inout
sq_camera move Wasser 1.3 0 -0.6 0.4
do_action rotate WasserfallPos 0
do_wait time 5

do_action anim breathe 0
do_wait time 0.5

do_action rotate left 0
do_wait time 0.7

sq_camera fix 0 0.8 0 -0.4
do_wait camera

#Fee text:Dank Dir, dass Du unserem Lebensbaum wieder Wasser geschenkt hast!
sq_actor actionlist 0 {{anim standloopa} {rotate 5.8} {rotate 0.3} {rotate 5.7} {rotate 0.5} {rotate 5.5} {rotate 0.5} {rotate 5.5}}
do_action rotate front 0
-do_fee text 010a Eben
do_wait time 7

do_fee move feen
do_action rotate right 0
do_wait time 0.4
do_action anim cheer 0
do_wait time 1.6


#Fee text:Dafuer sollst Du nicht ohne Lohn von dannen ziehen.
do_particle create 14 Baum1Pos {0.2 -0.2 0.03} 2 4
do_particle create 14 Baum2Pos {-0.3  -0.1 0.05} 3 4
do_particle create 14 Baum3Pos {-0.3 -0.2 0.05} 10 4
do_particle create 14 Baum4Pos {-0.1 -0.1 -0.3} 6 4
do_particle create 14 Baum5Pos {-0.2 -0.1 0.01} 2 4
do_particle create 14 Baum6Pos {0.2  -0.1 0.01} 2 4
do_particle create 14 Baum7Pos {0.2  -0.2 0.2} 2 4
do_particle create 14 Baum8Pos {0.2   0.2 0.01} 2 4
do_particle create 14 Baum9Pos  {0.1 0.1 0.1} 2 4
do_particle create 14 Baum10Pos {-0.1 -0.1 0.01} 2 4
do_particle create 14 Baum11Pos {-0.1 -0.1 0.01} 2 4

-do_fee text 010b Dafuer
sq_camera selset inout
sq_camera move Rechtscam 1.3 0 -0.7

sq_actor eyes 0 {9 9 9 9 9 9 9 9 9 9 9 9 c 9 9 9 9 9 9 9 9 9 c 9 9 9 9 9 9 9 9 c 9 9 9 9 9 9 9 c 9 9 9 9 9 9 c}
sq_actor mouth 0 {5}
do_wait camera
sq_wait 0

gametime factor 0.5
do_action anim teeter_t 0
+gametime factor 1
do_wait time 2

do_fee beam feen
do_wait time 2
do_fee radius {3}

sq_camera fix 0 0.8 0 -0.5
do_wait camera

sq_wait none
+sq_pen set PartPos Wasserfee2
+sq_pen move PartPos {0 -0.2 -2.5}
do_action rotate PartPos 0
do_wait time 0.4
do_action anim applaud 0
do_wait time 1

sq_actor actionlist 0 {{anim teeter_w} {anim standloopb} {anim teeter_w} {anim standloopc} {anim teeter_w}}
do_action anim standloopa 0
#Fee text:Nimm dies!
#do_fee text 010c
do_fee move Wasserfee2
do_wait time 2

gametime factor 0.5
sq_camera selset inout
sq_camera move Wassercam 1.1 -0.308 0.44
+gametime factor 1
do_wait time 4
#sq_pen set PartPos Wasserfee2
#sq_pen move PartPos {0 -0.1 -2.5}
#sq_pen set SchwertPos PartPos
#sq_pen move SchwertPos {-1 0 0}
do_action walk SchwertPos 0

-sound play magic_a 1

#sq_pen set PartPos1 PartPos
#sq_pen move PartPos1 {2 0 0}
#sq_pen set PartPos2 PartPos
#sq_pen move PartPos1 {0 -1 0}
#sq_pen set PartPos3 PartPos
#sq_pen move PartPos1 {2 -1 2}
#sq_pen set PartPos4 PartPos
#sq_pen move PartPos1 {1 -2 0}
#sq_pen set PartPos5 PartPos
#sq_pen move PartPos1 {0 -3 0}

do_particle create 13 PartPos {0 -0.1 0.01} 100 3
do_particle create 13 PartPos {0 -0.1 0.01} 100 3
do_particle create 13 PartPos {0 -0.1 0.01} 100 3
do_particle create 13 PartPos {0 -0.1 0.01} 100 3
do_particle create 13 PartPos {0 -0.1 0.01} 100 3

#do_particle create 33 PartPos1 {0 -0.5 0.01} 100 3
#do_particle create 33 PartPos2 {0 -0.1 0.01} 100 3
#do_particle create 33 PartPos3 {0 0.5 0.01} 100 3
#do_particle create 33 PartPos4 {0.1 -0.1 0.01} 100 3
#do_particle create 33 PartPos5 {0 -0.1 0.01} 100 3

sq_camera selset inout
sq_camera move Wassercam 0.8 -0.308 0.44
+sq_object summon Schwert_2 PartPos
set_rotx [Object 0] 1.57
do_wait time 5
sq_camera move Wassercam 1.5 -0.308 0.44

+sq_sound Nichts 0

+do_fee back
+do_action beam WasserBoden 0
#+sq_camera get

#010a Eben war das Bäumchen krank,|jetzt lebt es wieder, Zwerg/Gott sei Dank.
#010b Dafür gibt es jetzt mein Sohn,|dieses dicke Schwert zum Lohn!



