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

+do_elf beam Rechts
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
sq_camera move CamMitte 1.4 -0.1 0.2 0.25
do_wait camera

+do_elf hide
+sq_camera move CamMitte 1.94 -0.125 0.233 0.7
do_wait time 3

do_wait time 3
