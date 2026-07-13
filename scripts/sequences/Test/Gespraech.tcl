sq_camera follow 0 1
do_wait camera
sq_wait all
do_action walk TriggerPos 0
sq_actor find Zwerg 200 3
sq_pen set Z1 0
sq_pen move Z1 {1 0 0}
sq_wait all
do_action walk Z1 1
sq_wait none
do_action rotate 0 1
sq_wait all
do_action rotate 1 0
sq_wait none
sq_pen move Z1 {-0.5 0 1}
do_action walk Z1 2
sq_color 0 Red
sq_color 1 Yellow
sq_wait {0 1}
sq_camera fix  0 0.8 -0.3 -0.2
do_text "Hi!" 0
sq_camera fix  1 0.75 -0.3 0.2
do_text "Tach auch!" 1
do_text "Hey, werd 'ma nich frech hier Kleiner" 0
sq_color 1 White
sq_color 0 White
do_text "Kuck mal, ich kann auch mit weiﬂer Schrift sprechen" 1
sq_wait none
do_text "Na und, ich auch" 0
sq_wait all
do_action rotate back 2
sq_color 0 Red
sq_color 1 Yellow
sq_color 2 Green
do_text "Was labert ihr hier rum?|Geht lieber an die Arbeit!" 2
do_text "Ja,ja..|Ist ja gut.." {0 1}


do_wait