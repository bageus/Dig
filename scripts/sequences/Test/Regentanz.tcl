##Regentanz
do_wait camera

#sq_actor find Stein 100 2
sq_actor find Zwerg 100 1
sq_pen set z 1
#sq_pen set pos2 2
#sq_pen set z 3
sq_wait elf
do_elf move z
do_elf text "Jetzt move zum Stein2"
#do_elf move pos2
do_elf text "Jetzt lookat Zwerg"
#do_elf lookat 3
do_elf text "Jetzt path zum Stei1, speed 0.3"
#do_elf path pos2 pos1 0.3
do_elf text "Jetzt anim verzweifeln"
do_elf anim verzweifeln
do_elf text "nach 5 sek. hide ich"
do_wait time 5
+do_elf hide

