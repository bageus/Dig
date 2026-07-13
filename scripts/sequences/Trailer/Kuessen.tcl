#Schwert zum Himmel
# sq_camera follow 0 1
sq_wait all
sq_actor find Zwerg 20 2
do_action beam {105.65 34.375 12.7} 0
do_action beam {104.7 34.375 13.0} 1
# sq_wait none
do_action rotate left 0
do_action rotate right 1
do_wait
do_action anim kiss all
do_wait
sq_wait all
print "Done."
