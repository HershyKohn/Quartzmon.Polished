-- public.qrtz_execution_history_entries definition

DROP TABLE IF EXISTS public.qrtz_execution_history_entries ;

CREATE TABLE IF NOT EXISTS public.qrtz_execution_history_entries (
	fire_instance_id varchar(140) NOT NULL,
	scheduler_instance_id varchar(200) NOT NULL,
	sched_name varchar(120) NOT NULL,
	job_name varchar(150) NOT NULL,
	trigger_name varchar(150) NOT NULL,
	scheduled_fire_time_utc timestamp NULL,
	actual_fire_time_utc timestamp NOT NULL,
	recovering bool NOT NULL DEFAULT false,
	vetoed bool NOT NULL DEFAULT false,
	finished_time_utc timestamp NULL,
	exception_message text NULL,
	trial168 bpchar(1) NULL,
	CONSTRAINT pk_qrtz_execution_history_entries PRIMARY KEY (fire_instance_id)
);
CREATE INDEX ix_actual_fire_time_utc ON public.qrtz_execution_history_entries USING btree (actual_fire_time_utc);
CREATE INDEX ix_job_name_actual_fire_time_utc ON public.qrtz_execution_history_entries USING btree (job_name, actual_fire_time_utc);
CREATE INDEX ix_sched_name ON public.qrtz_execution_history_entries USING btree (sched_name);
CREATE INDEX ix_trigger_name_actual_fire_time_utc ON public.qrtz_execution_history_entries USING btree (trigger_name, actual_fire_time_utc);


-- public.qrtz_execution_history_stats definition

DROP TABLE IF EXISTS public.qrtz_execution_history_stats;

CREATE TABLE IF NOT EXISTS public.qrtz_execution_history_stats (
	sched_name varchar(120) NOT NULL,
	stat_name varchar(120) NOT NULL,
	stat_value int8 NULL,
	trial171 bpchar(1) NULL,
	CONSTRAINT pk_execution_history_stats PRIMARY KEY (sched_name, stat_name)
);