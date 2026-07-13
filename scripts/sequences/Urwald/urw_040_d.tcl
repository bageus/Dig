#Clip 40d (nicht im Drehbuch) - Tor schließt sich und es sind schon alle drüben

sq_pen set imTor [Getobjpos Riesentor 0]
sq_pen move imTor {0.5 0 -5}

+set_anim [Getobjref Riesentor 0] riesentor.offen 0 0

sq_wait all

+sq_camera fix imTor 1.3 -0.2 -0.7

#da kracht sie zu
do_wait time 2
set_anim [Getobjref Riesentor 0] riesentor.schliessen 0 1
do_wait time 2
+set_anim [Getobjref Riesentor 0] riesentor.zu 0 0
+call_method [Getobjref Riesentor] set_closed
do_wait time 1

#kein sq_camera get, hier gibts ja nix mehr zu sehen
