foreach item [obj_query 0 "-class {Zwerg Zuschauer}"] {set_sequenceactive $item 1}
sq_camera fix  {149.2 32.9 14} 1.0 -0.21 0.635
do_wait time 0.5
sq_camera move {169.2 32.5 14} 1.0 -0.27 0.41
do_wait time 7
sq_camera move {169.2 32.5 14} 0.6 -0.27 0.41
do_wait time 0.5
call_method /obj/Zu_1 invoke_laola
do_wait time 1.5

sq_camera move {180.6 32.5 14} 1 -0.29 0.5
do_wait time 2
call_method /obj/Zu_2 invoke_laola
sq_camera move {184.6 32.5 14} 1 -0.29 0.5
do_wait time 2
sq_camera move {190.6 32.5 14} 1 -0.29 0.5
do_wait time 2
sq_camera move {194.6 32.5 14} 1 -0.29 0.5
call_method /obj/Zu_3 invoke_laola
do_wait time 2
sq_camera move {196.6 32.5 14} 1 -0.29 0.4
do_wait time 2
sq_camera move {200.6 32.5 14} 1 -0.29 0.4
call_method /obj/Zu_4 invoke_laola
do_wait time 2
sq_camera move {204.6 32.5 14} 1 -0.29 0.4
do_wait time 2
sq_camera move {210.6 32.5 14} 1 -0.29 0.4
do_wait time 2
sq_camera move {216.6 32.5 14} 1 -0.29 0.4
do_wait time 2
sq_camera move {220.6 32.5 14} 1 -0.29 0.3
do_wait time 2
call_method /obj/Zu_5 invoke_laola
sq_camera move {224.6 32.5 14} 1 -0.29 0.2
do_wait time 2
sq_camera move {228.6 32.5 14} 1.1 -0.29 0.1
do_wait time 2
sq_camera move {233.9 32.5 14} 1.2 -0.29 0.0
do_wait time 2
call_method /obj/Zu_6 invoke_laola
do_wait time 2
sq_camera move {228.6 32.5 14} 1.1 -0.29 0.1
do_wait time 2
sq_camera move {227.0 32.0 14} 0.6 -0.29 0.1
do_wait time 4
call_method /obj/Zu_6 invoke_laola


do_wait time 2

 