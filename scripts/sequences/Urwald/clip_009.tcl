sq_text file Urwald

	+sq_pen set Schalter [Getobjpos Schalter_knopf_stein_1]
	+sq_pen set SchalterZwerg Schalter
	+sq_pen move SchalterZwerg {0 0.5 0}

    +sq_pen set Tuer [Getobjpos Tuer_kaserne]

    	+sq_pen set Tuere Tuer
    	+sq_pen move Tuere {0.4 -0.4 0}

    +sq_pen set Mitte [Getobjpos Info_Pos_Troll]

		+sq_pen set CamObenMitte Mitte
		+sq_pen move CamObenMitte {0.2 -10 0}

		+sq_pen set CamAustritt Mitte
		+sq_pen move CamAustritt {0.2 -5 0}

    	+sq_pen set CamMitte Mitte
    	+sq_pen move CamMitte {0.2 0.2 0}



    +sq_pen set Rechts [Getobjpos Info_Pos_ZwergTmp]


do_wait time 0.1
sq_camera move Tuere 2 -0.3 -0.2 0.5
do_wait camera

#sq_camera selset inout
#sq_camera move CamObenMitte 1.4 -0.1 0.2 0.4
#do_wait camera

#sq_camera selset inout
#sq_camera move CamAustritt 1.4 -0.1 0.2 0.3
#do_wait time 4.5

sq_camera selset inout
#sq_camera move CamMitte 1.4 -0.1 0.2 0.1
+sq_pen set CamMitte2 CamMitte
+sq_pen move CamMitte2 {0 -2 0}
sq_camera move CamMitte2 1.4 -0.1 0.2 0.1

+sq_pen set WaterfallPos CamMitte
+sq_pen move WaterfallPos {-2.5 -3 11}
+sq_pen set WaterPos CamMitte
+sq_pen move WaterPos {-2.2 -12 6}

+sq_pen set WaterStream1 WaterPos
+sq_pen move WaterStream1 {-0.5 0 0}
+sq_pen set WaterStream2 WaterPos
+sq_pen move WaterStream2 {0.5 0 0}
+sq_pen set WaterStream3 WaterPos
+sq_pen move WaterStream3 {0.25 0 0}
+sq_pen set WaterStream4 WaterPos
+sq_pen move WaterStream4 {-0.25 0 0}
+sq_pen set WaterStream5 WaterPos
+sq_pen move WaterStream5 {0.15 0 0}
+sq_pen set WaterStream6 WaterPos
+sq_pen move WaterStream6 {-0.15 0 0}
+sq_pen set WaterStream7 WaterPos
+sq_pen move WaterStream7 {0.07 0 0}
+sq_pen set WaterStream8 WaterPos
+sq_pen move WaterStream8 {-0.07 0 0}
+sq_pen set WaterStream9 WaterPos
+sq_pen move WaterStream9 {-0.5 0 0}
+sq_pen set WaterStream10 WaterPos
+sq_pen move WaterStream10 {0.5 0 0}
+sq_pen set WaterStream11 WaterPos
+sq_pen move WaterStream11 {0.25 0 0}
+sq_pen set WaterStream12 WaterPos
+sq_pen move WaterStream12 {-0.25 0 0}
+sq_pen set WaterStream13 WaterPos
+sq_pen move WaterStream13 {0.15 0 0}
+sq_pen set WaterStream14 WaterPos
+sq_pen move WaterStream14 {-0.15 0 0}
+sq_pen set WaterStream15 WaterPos
+sq_pen move WaterStream15 {0.07 0 0}
+sq_pen set WaterStream16 WaterPos
+sq_pen move WaterStream16 {-0.07 0 0}


+sq_pen set WaterBlubb1 WaterStream1
+sq_pen move WaterBlubb1 {0.25 15 0}
+sq_pen set WaterBlubb2 WaterStream2
+sq_pen move WaterBlubb2 {0.25 15 0}
+sq_pen set WaterBlubb3 WaterStream3
+sq_pen move WaterBlubb3 {0.25 15 0}
+sq_pen set WaterBlubb4 WaterStream4
+sq_pen move WaterBlubb4 {0.25 15 0}
+sq_pen set WaterBlubb5 WaterStream5
+sq_pen move WaterBlubb5 {0.25 15 0}
+sq_pen set WaterBlubb6 WaterStream6
+sq_pen move WaterBlubb6 {0.25 15 0}

+sq_pen set WaterSpritzer1 WaterStream1
+sq_pen move WaterSpritzer1 {0.25 12 0}
+sq_pen set WaterSpritzer2 WaterStream2
+sq_pen move WaterSpritzer2 {0.25 12 0}
+sq_pen set WaterSpritzer3 WaterStream3
+sq_pen move WaterSpritzer3 {0.25 12 0}
+sq_pen set WaterSpritzer4 WaterStream4
+sq_pen move WaterSpritzer4 {0.25 12 0}
+sq_pen set WaterSpritzer5 WaterStream5
+sq_pen move WaterSpritzer5 {0.25 12 0}
+sq_pen set WaterSpritzer6 WaterStream6
+sq_pen move WaterSpritzer6 {0.25 12 0}


do_particle create 7 WaterStream1 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream2 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream3 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream4 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream5 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream6 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream7 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream8 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream9 {0 0.4 0.1 } 8 2
do_particle create 7 WaterStream10 {0 0.4 0.1 } 8 1.5
do_particle create 7 WaterStream11 {0 0.4 0.1 } 8 1.5
do_particle create 7 WaterStream12 {0 0.4 0.1 } 8 1.5
do_particle create 7 WaterStream13 {0 0.4 0.1 } 8 1.5
do_particle create 7 WaterStream14 {0 0.4 0.1 } 8 1.5
do_particle create 7 WaterStream15 {0 0.4 0.1 } 8 1.5
do_particle create 7 WaterStream16 {0 0.4 0.1 } 8 1.5


do_particle create 23 WaterBlubb1 {0 -0.05 0} 4 6
do_particle create 23 WaterBlubb2 {0 -0.05 0} 4 6
do_particle create 23 WaterBlubb3 {0 -0.05 0} 4 6
do_particle create 23 WaterBlubb4 {0 -0.05 0} 4 6
do_particle create 23 WaterBlubb5 {0 -0.05 0} 4 6
do_particle create 23 WaterBlubb6 {0 -0.05 0} 4 6

sq_object summon Dummy_Urw_wasserfall_a WaterfallPos
do_wait camera


+sq_camera move CamMitte2 1.74 -0.125 0.233 0.7

#+sq_camera move CamMitte 1.94 -0.125 0.233 0.7

do_wait time 6

