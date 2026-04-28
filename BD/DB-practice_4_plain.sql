--
-- PostgreSQL database dump
--

\restrict YV9thd3NCwSBghy7vs7UlhxPsiX74eDoYkHknQVAEKlpygqyLPf58MBKYA8SVhC

-- Dumped from database version 16.10
-- Dumped by pg_dump version 18.0

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: a; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.a (
    id integer
);


ALTER TABLE public.a OWNER TO postgres;

--
-- Data for Name: a; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.a (id) FROM stdin;
401
301
301
801
801
901
901
\.


--
-- Name: a_id_idx; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX a_id_idx ON public.a USING btree (id);


--
-- PostgreSQL database dump complete
--

\unrestrict YV9thd3NCwSBghy7vs7UlhxPsiX74eDoYkHknQVAEKlpygqyLPf58MBKYA8SVhC

